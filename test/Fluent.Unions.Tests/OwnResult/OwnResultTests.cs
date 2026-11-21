using System.Diagnostics;
using Fluent.Unions;
using Is = NUnit.Framework.Is;

namespace OwnResultSample.Tests;

public class OwnResultTests
{
    private static Result<int> Find(int id) => id > 0 ? id : new NotFoundException($"No order {id}.");

    private static Outcome<int> FindOutcome(int id) => id > 0 ? id : new NotFoundException($"No order {id}.");

    private static string Describe<T>(Result<T> result) => result.Match(value => $"ok {value}", error => $"{error.GetType().Name}");

    [Test]
    public void CasesConvertAndMatch()
    {
        string Status(Result<int> result) => result switch
        {
            int value => $"value {value}",
            NotFoundException => "not found",
            DomainException error => error.Message
        };

        Assert.That(Status(Find(1)), Is.EqualTo("value 1"));
        Assert.That(Status(Find(0)), Is.EqualTo("not found"));
    }

    [Test]
    public void ExceptionThatIsNotADomainExceptionGoesThroughFromException()
    {
        var thrown = Result.From(1).Select<int, int>(_ => throw new IOException("disk"));

        Assert.That(thrown.GetError(), Is.TypeOf<UnexpectedDomainException>());
        Assert.That(thrown.GetError().InnerException, Is.TypeOf<IOException>());
    }

    [Test]
    public void DomainExceptionThrownInAStepIsKept()
    {
        var thrown = Result.From(1).Select<int, int>(_ => throw new OutOfStockException("none left"));

        Assert.That(thrown.GetError(), Is.TypeOf<OutOfStockException>());
    }

    [Test]
    public void CancellationPropagates() =>
        Assert.Throws<OperationCanceledException>(() => Result.From(1).Select<int, int>(_ => throw new OperationCanceledException()));

    [Test]
    public void DefaultResultIsAnError()
    {
        var error = default(Result<int>).GetError();

        Assert.That(error, Is.TypeOf<UnexpectedDomainException>());
        Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void EnsureWithAMessageMapsTheValidationError()
    {
        var error = Result.From("").Ensure(text => text.Length > 0, "Text is empty.").GetError();

        Assert.That(error.InnerException, Is.TypeOf<ValidationError>());
        Assert.That(error.Message, Is.EqualTo("Text is empty."));
    }

    [Test]
    public void ValidateCollectsEveryFailure()
    {
        var result = Result.From(5).Validate(
            n => n > 10 ? Result.Ok : Result.Error(new DomainException("too small")),
            n => n % 2 == 0 ? Result.Ok : Result.Error(new DomainException("odd")));

        Assert.That(result.GetError().InnerException, Is.TypeOf<AggregateException>());
    }

    [Test]
    public void TupleAndFlatteningOperatorsKeepTheType()
    {
        Result<(int, string)> both = Find(1).BindAppend(n => Result.From(n.ToString()));
        Result<(int First, int Second)> zipped = Find(1).Zip(Find(2));
        Result<int> flat = Result.From(Find(3)).Unwrap();
        Result unit = Find(4);

        Assert.That(both.GetValue(), Is.EqualTo((1, "1")));
        Assert.That(zipped.GetValue().Second, Is.EqualTo(2));
        Assert.That(flat.GetValue(), Is.EqualTo(3));
        Assert.That(unit.IsSuccess, Is.True);
        Assert.That(unit.WithValue("again").GetValue(), Is.EqualTo("again"));
    }

    [Test]
    public void CollectionOperatorsWork()
    {
        Result<int>[] results = [Find(1), Find(0), Find(2)];

        Assert.That(results.Combine().GetError(), Is.TypeOf<NotFoundException>());
        Assert.That(results.Values(), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(results.Errors().Single(), Is.TypeOf<NotFoundException>());
        Assert.That(results.Partition().Values, Is.EqualTo(new[] { 1, 2 }));
        Assert.That(new[] { Find(1), Find(2) }.CombineAll().GetValue(), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task CompanionStaticsWork()
    {
        Assert.That(Result.Try(() => int.Parse("12")).GetValue(), Is.EqualTo(12));
        Assert.That(Result.Try(() => int.Parse("x")).GetError().InnerException, Is.TypeOf<FormatException>());
        Assert.That(Result.Try(() => { }).IsSuccess, Is.True);
        Assert.That((await Result.TryAsync(() => Task.FromResult(3))).GetValue(), Is.EqualTo(3));

        var attempts = 0;
        Assert.That(Result.Retry(() => ++attempts < 3 ? Find(0) : Find(attempts), maxAttempts: 5).GetValue(), Is.EqualTo(3));

        var all = await Result.WhenAll([Task.FromResult(Find(1)), Task.FromResult(Find(2))]);
        Assert.That(all.GetValue(), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(Result.CombineAll(Find(1), Find(2)).GetValue(), Is.EqualTo((1, 2)));
    }

    [Test]
    public async Task AsyncPipelineStaysOneExpression()
    {
        var length = await Task.FromResult(Result.From("abcd"))
            .Select(text => text.Length)
            .ChainAwait(n => Task.FromResult(Result.From(n * 2)))
            .Match(n => n, _ => -1);

        Assert.That(length, Is.EqualTo(8));
    }

    [Test]
    public async Task FaultedTaskBecomesAMappedError()
    {
        var result = await Task.FromException<Result<int>>(new IOException("down")).Select(n => n + 1);

        Assert.That(result.GetError(), Is.TypeOf<UnexpectedDomainException>());
    }

    [Test]
    public void OutCapturesTheValue()
    {
        var doubled = Find(21).Out(out var original).Select(n => n * 2);

        Assert.That(original.Value + doubled.GetValue(), Is.EqualTo(63));
    }

    [Test]
    public void TypedCatchHandlesOnlyItsType()
    {
        Assert.That(Find(0).Catch((NotFoundException _) => Result<int>.Ok(-1)).GetValue(), Is.EqualTo(-1));
        Assert.That(Describe(Find(0).Catch((OutOfStockException _) => Result<int>.Ok(-1))), Is.EqualTo(nameof(NotFoundException)));
    }

    [Test]
    public void CheckRethrowsTheDomainException() =>
        Assert.Throws<NotFoundException>(() => Find(0).Check());

    [Test]
    public async Task ConversionsWork()
    {
        Assert.That(Find(1).ToOption().GetValue(), Is.EqualTo(1));
        Assert.That(Find(0).ToLoadable().IsFaulted, Is.True);
        Assert.That(Result.From((Option<int>)5).Transpose().GetValue().GetValue(), Is.EqualTo(5));

        var timedOut = await new TaskCompletionSource<Result<int>>().Task.Timeout(TimeSpan.FromMilliseconds(10));
        Assert.That(timedOut.GetError().InnerException, Is.TypeOf<TimeoutException>());

        using var activity = new Activity("own");
        Assert.That(activity.RecordResult(Find(0))?.Status, Is.EqualTo(ActivityStatusCode.Error));
    }

    [Test]
    public void ClosedErrorUnionIsCheckedExhaustively()
    {
        int Status(Outcome<int> outcome) => outcome.Match(_ => 200, error => error switch
        {
            NotFoundException => 404,
            OutOfStockException => 409,
            Unexpected => 500
        });

        Assert.That(Status(FindOutcome(1)), Is.EqualTo(200));
        Assert.That(Status(FindOutcome(0)), Is.EqualTo(404));
        Assert.That(Status(Outcome.Try(() => int.Parse("x"))), Is.EqualTo(500));
    }

    [Test]
    public void CatchMatchesACaseOfTheErrorUnion() =>
        Assert.That(FindOutcome(0).Catch((NotFoundException _) => Outcome<int>.Ok(7)).GetValue(), Is.EqualTo(7));

    [Test]
    public void CheckThrowsTheExceptionCaseOfAnErrorUnion() =>
        Assert.Throws<NotFoundException>(() => FindOutcome(0).Check());

    [Test]
    public void CheckWrapsACaseThatIsNotAnException() =>
        Assert.Throws<ResultErrorException>(() => Outcome.Try<int>(() => throw new IOException("x")).Check());

    [Test]
    public void OperatorsDoNotAllocate()
    {
        Result<int> number = 1;
        Result<string> text = "x";

        Assert.That(Allocations(() => number.Select(static n => n + 1).GetValueOrDefault()), Is.Zero);
        Assert.That(Allocations(() => text.Chain(static s => Result<string>.Ok(s)).IsSuccess ? 1 : 0), Is.Zero);
        Assert.That(Allocations(() => ((Result<int>)42).GetValueOrDefault()), Is.Zero);
    }

    [Test]
    public void DefaultResultTypeDoesNotBoxValuePayloads()
    {
        Fluent.Unions.Result<int> number = 1;

        Assert.That(Allocations(() => ((Fluent.Unions.Result<int>)42).GetValueOrDefault()), Is.Zero);
        Assert.That(Allocations(() => number.Select(static n => n + 1).GetValueOrDefault()), Is.Zero);
    }

    private static long Allocations(Func<int> operation)
    {
        var lowest = long.MaxValue;
        for (var attempt = 0; attempt < 60 && lowest > 0; attempt++)
        {
            for (var i = 0; i < 10_000; i++)
                operation();

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++)
                operation();

            lowest = Math.Min(lowest, GC.GetAllocatedBytesForCurrentThread() - before);
            if (lowest > 0)
                Thread.Sleep(200);
        }

        return lowest;
    }
}
