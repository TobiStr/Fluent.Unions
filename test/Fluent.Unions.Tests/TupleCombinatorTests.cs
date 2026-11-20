namespace Fluent.Unions.Tests;

public class TupleCombinatorTests
{
    private static Result<int> Empty => default;

    private static Result<int> Fail(string message) => new InvalidOperationException(message);

    [Test]
    public void CombineAllOfAllOkGivesTheTuple()
    {
        Result<(int, string)> two = Result.CombineAll(Result<int>.Ok(1), Result<string>.Ok("a"));
        Result<(int, string, bool)> three = Result.CombineAll(Result<int>.Ok(1), Result<string>.Ok("a"), Result<bool>.Ok(true));
        Result<(int, string, bool, char)> four = Result.CombineAll(
            Result<int>.Ok(1), Result<string>.Ok("a"), Result<bool>.Ok(true), Result<char>.Ok('x'));

        Assert.That(two.GetValue(), Is.EqualTo((1, "a")));
        Assert.That(three.GetValue(), Is.EqualTo((1, "a", true)));
        Assert.That(four.GetValue(), Is.EqualTo((1, "a", true, 'x')));
    }

    [Test]
    public void CombineAllCollectsEveryErrorInOneFlatAggregate()
    {
        Result<(int, int)> two = Result.CombineAll(Fail("first"), Fail("second"));
        Result<(int, int, int)> three = Result.CombineAll(Fail("first"), Result<int>.Ok(1), Fail("third"));
        Result<(int, int, int, int)> four = Result.CombineAll(Fail("a"), Fail("b"), Result<int>.Ok(1), Fail("d"));

        var twoErrors = (AggregateException)two.GetError();
        var threeErrors = (AggregateException)three.GetError();
        var fourErrors = (AggregateException)four.GetError();

        Assert.That(twoErrors.InnerExceptions.Select(e => e.Message), Is.EqualTo(new[] { "first", "second" }));
        Assert.That(threeErrors.InnerExceptions.Select(e => e.Message), Is.EqualTo(new[] { "first", "third" }));
        Assert.That(fourErrors.InnerExceptions.Select(e => e.Message), Is.EqualTo(new[] { "a", "b", "d" }));
    }

    [Test]
    public void CombineAllFlattensNestedAggregates()
    {
        Result<int> nested = new AggregateException(new Exception("one"), new Exception("two"));

        Result<(int, int)> result = Result.CombineAll(nested, Fail("three"));

        var error = (AggregateException)result.GetError();
        Assert.That(error.InnerExceptions.Select(e => e.Message), Is.EquivalentTo(new[] { "one", "two", "three" }));
    }

    [Test]
    public void CombineAllOfASingleErrorIsStillAnAggregate()
    {
        Result<(int, int)> result = Result.CombineAll(Result<int>.Ok(1), Fail("only"));

        Assert.That(result.GetError(), Is.TypeOf<AggregateException>());
    }

    [Test]
    public void CombineAllPrefersTheErrorOverAnEmptySide()
    {
        Result<(int, int)> two = Result.CombineAll(Empty, Fail("broken"));
        Result<(int, int, int)> three = Result.CombineAll(Empty, Fail("broken"), Result<int>.Ok(1));
        Result<(int, int, int, int)> four = Result.CombineAll(Result<int>.Ok(1), Empty, Result<int>.Ok(2), Fail("broken"));

        Assert.That(two.GetError(), Is.TypeOf<AggregateException>());
        Assert.That(three.GetError(), Is.TypeOf<AggregateException>());
        Assert.That(four.GetError(), Is.TypeOf<AggregateException>());
    }

    [Test]
    public void CombineAllOfOkAndDefaultIsAnError()
    {
        Result<(int, int)> two = Result.CombineAll(Result<int>.Ok(1), Empty);
        Result<(int, int, int)> three = Result.CombineAll(Result<int>.Ok(1), Result<int>.Ok(2), Empty);
        Result<(int, int, int, int)> four = Result.CombineAll(Empty, Result<int>.Ok(1), Result<int>.Ok(2), Result<int>.Ok(3));

        Assert.That(two.GetError().Message, Does.Contain("holds neither a value nor an error"));
        Assert.That(three.GetError().Message, Does.Contain("holds neither a value nor an error"));
        Assert.That(four.GetError().Message, Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public async Task CombineAllAsyncAwaitsEveryTask()
    {
        Result<(int, string)> two = await Result.CombineAllAsync(
            Task.FromResult(Result<int>.Ok(1)), Task.FromResult(Result<string>.Ok("a")));
        Result<(int, string, bool)> three = await Result.CombineAllAsync(
            Task.FromResult(Result<int>.Ok(1)), Task.FromResult(Result<string>.Ok("a")), Task.FromResult(Result<bool>.Ok(true)));
        Result<(int, string, bool, char)> four = await Result.CombineAllAsync(
            Task.FromResult(Result<int>.Ok(1)),
            Task.FromResult(Result<string>.Ok("a")),
            Task.FromResult(Result<bool>.Ok(true)),
            Task.FromResult(Result<char>.Ok('x')));

        Assert.That(two.GetValue(), Is.EqualTo((1, "a")));
        Assert.That(three.GetValue(), Is.EqualTo((1, "a", true)));
        Assert.That(four.GetValue(), Is.EqualTo((1, "a", true, 'x')));
    }

    [Test]
    public async Task CombineAllAsyncTurnsAFaultedTaskIntoACollectedError()
    {
        Result<(int, int, int)> result = await Result.CombineAllAsync(
            Task.FromException<Result<int>>(new TimeoutException("slow")),
            Task.FromResult(Result<int>.Ok(1)),
            Task.FromResult(Fail("broken")));

        var error = (AggregateException)result.GetError();
        Assert.That(error.InnerExceptions.Select(e => e.GetType()), Is.EqualTo(new[] { typeof(TimeoutException), typeof(InvalidOperationException) }));
    }

    [Test]
    public async Task CombineAllAsyncReportsDefaultAsError()
    {
        Result<(int, int)> result = await Result.CombineAllAsync(Task.FromResult(Result<int>.Ok(1)), Task.FromResult(Empty));

        Assert.That(result.GetError().Message, Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void BindAppendAddsEachArity()
    {
        Result<(int, string)> two = Result<int>.Ok(1).BindAppend(v => Result<string>.Ok($"n{v}"));
        Result<(int, string, bool)> three = two.BindAppend((n, s) => Result<bool>.Ok(s.Length > n));
        Result<(int, string, bool, char)> four = three.BindAppend((n, s, b) => Result<char>.Ok(b ? s[0] : 'x'));

        Assert.That(two.GetValue(), Is.EqualTo((1, "n1")));
        Assert.That(three.GetValue(), Is.EqualTo((1, "n1", true)));
        Assert.That(four.GetValue(), Is.EqualTo((1, "n1", true, 'n')));
    }

    [Test]
    public void BindAppendPassesErrorAndDefaultThroughWithoutCallingTheSelector()
    {
        var calls = 0;
        Result<(int, int)> twoError = new InvalidOperationException("broken");
        Result<(int, int)> twoEmpty = default;
        Result<(int, int, int)> threeError = new InvalidOperationException("broken");
        Result<(int, int, int)> threeEmpty = default;

        Result<(int, int)> a = Fail("broken").BindAppend(v => { calls++; return Result<int>.Ok(v); });
        Result<(int, int)> b = Empty.BindAppend(v => { calls++; return Result<int>.Ok(v); });
        Result<(int, int, int)> c = twoError.BindAppend((x, y) => { calls++; return Result<int>.Ok(x); });
        Result<(int, int, int)> d = twoEmpty.BindAppend((x, y) => { calls++; return Result<int>.Ok(x); });
        Result<(int, int, int, int)> e = threeError.BindAppend((x, y, z) => { calls++; return Result<int>.Ok(x); });
        Result<(int, int, int, int)> f = threeEmpty.BindAppend((x, y, z) => { calls++; return Result<int>.Ok(x); });

        Assert.That(a.GetError().Message, Is.EqualTo("broken"));
        Assert.That(c.GetError().Message, Is.EqualTo("broken"));
        Assert.That(e.GetError().Message, Is.EqualTo("broken"));
        Assert.That(new[] { b.GetError().Message, d.GetError().Message, f.GetError().Message }, Is.All.Contain("holds neither a value nor an error"));
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void BindAppendWithAnErrorOrDefaultSelectorResult()
    {
        Result<(int, int)> error = Result<int>.Ok(1).BindAppend(_ => Fail("broken"));
        Result<(int, int)> empty = Result<int>.Ok(1).BindAppend(_ => Empty);
        Result<(int, int)> thrown = Result<int>.Ok(1).BindAppend<int, int>(_ => throw new FormatException());

        Assert.That(error.GetError().Message, Is.EqualTo("broken"));
        Assert.That(empty.GetError().Message, Does.Contain("holds neither a value nor an error"));
        Assert.That(thrown.GetError(), Is.TypeOf<FormatException>());
    }

    [Test]
    public async Task BindAppendOnATaskReceiver()
    {
        Result<(int, string, bool, char)> result = await Task.FromResult(Result<int>.Ok(1))
            .BindAppend(v => Result<string>.Ok($"n{v}"))
            .BindAppend((n, s) => Result<bool>.Ok(true))
            .BindAppend((n, s, b) => Result<char>.Ok('x'));
        Result<(int, int)> faulted = await Task.FromException<Result<int>>(new TimeoutException())
            .BindAppend(v => Result<int>.Ok(v));

        Assert.That(result.GetValue(), Is.EqualTo((1, "n1", true, 'x')));
        Assert.That(faulted.GetError(), Is.TypeOf<TimeoutException>());
    }

    [Test]
    public async Task BindAppendAwaitAddsEachArity()
    {
        Result<(int, string, bool, char)> result = await Result<int>.Ok(1)
            .BindAppendAwait(v => Task.FromResult(Result<string>.Ok($"n{v}")))
            .BindAppendAwait((n, s) => Task.FromResult(Result<bool>.Ok(true)))
            .BindAppendAwait((n, s, b) => Task.FromResult(Result<char>.Ok('x')));

        Assert.That(result.GetValue(), Is.EqualTo((1, "n1", true, 'x')));
    }

    [Test]
    public async Task BindAppendAwaitPassesThroughAndCapturesFailures()
    {
        var calls = 0;
        Result<(int, int)> twoError = new InvalidOperationException("broken");
        Result<(int, int, int)> threeEmpty = default;

        Result<(int, int)> error = await Fail("broken").BindAppendAwait(v => { calls++; return Task.FromResult(Result<int>.Ok(v)); });
        Result<(int, int)> empty = await Empty.BindAppendAwait(v => { calls++; return Task.FromResult(Result<int>.Ok(v)); });
        Result<(int, int, int)> three = await twoError.BindAppendAwait((x, y) => { calls++; return Task.FromResult(Result<int>.Ok(x)); });
        Result<(int, int, int, int)> four = await threeEmpty.BindAppendAwait((x, y, z) => { calls++; return Task.FromResult(Result<int>.Ok(x)); });
        Result<(int, int)> thrown = await Result<int>.Ok(1).BindAppendAwait<int, int>(_ => throw new FormatException());
        Result<(int, int)> faulted = await Result<int>.Ok(1).BindAppendAwait(_ => Task.FromException<Result<int>>(new TimeoutException()));
        Result<(int, int)> emptySelector = await Result<int>.Ok(1).BindAppendAwait(_ => Task.FromResult(Empty));

        Assert.That(error.GetError().Message, Is.EqualTo("broken"));
        Assert.That(three.GetError().Message, Is.EqualTo("broken"));
        Assert.That(new[] { empty.GetError().Message, four.GetError().Message, emptySelector.GetError().Message }, Is.All.Contain("holds neither a value nor an error"));
        Assert.That(thrown.GetError(), Is.TypeOf<FormatException>());
        Assert.That(faulted.GetError(), Is.TypeOf<TimeoutException>());
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public async Task BindAppendAwaitOnATaskReceiver()
    {
        Result<(int, string, bool, char)> result = await Task.FromResult(Result<int>.Ok(1))
            .BindAppendAwait(v => Task.FromResult(Result<string>.Ok($"n{v}")))
            .BindAppendAwait((n, s) => Task.FromResult(Result<bool>.Ok(true)))
            .BindAppendAwait((n, s, b) => Task.FromResult(Result<char>.Ok('x')));
        Result<(int, int)> faulted = await Task.FromException<Result<int>>(new TimeoutException())
            .BindAppendAwait(v => Task.FromResult(Result<int>.Ok(v)));

        Assert.That(result.GetValue(), Is.EqualTo((1, "n1", true, 'x')));
        Assert.That(faulted.GetError(), Is.TypeOf<TimeoutException>());
    }

    [Test]
    public void WithValueHandlesSuccessErrorAndDefault()
    {
        Result<int> ok = Result.Ok.WithValue(7);
        Result<int> error = Result.Error(new InvalidOperationException("broken")).WithValue(7);
        Result<int> empty = default(Result).WithValue(7);

        Assert.That(ok.GetValue(), Is.EqualTo(7));
        Assert.That(error.GetError().Message, Is.EqualTo("broken"));
        Assert.That(empty.GetError().Message, Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void WithValueRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Ok.WithValue<string>(null!));
    }

    [Test]
    public async Task WithValueOnATask()
    {
        Result<int> ok = await Task.FromResult(Result.Ok).WithValue(7);
        Result<int> empty = await Task.FromResult(default(Result)).WithValue(7);
        Result<int> faulted = await Task.FromException<Result>(new TimeoutException()).WithValue(7);

        Assert.That(ok.GetValue(), Is.EqualTo(7));
        Assert.That(empty.GetError().Message, Does.Contain("holds neither a value nor an error"));
        Assert.That(faulted.GetError(), Is.TypeOf<TimeoutException>());
        Assert.Throws<ArgumentNullException>(() => Task.FromResult(Result.Ok).WithValue<string>(null!));
    }

    [Test]
    public void DiscardValueHandlesSuccessErrorAndDefault()
    {
        Result ok = Result<int>.Ok(7).DiscardValue();
        Result error = Fail("broken").DiscardValue();
        Result empty = Empty.DiscardValue();

        Assert.That(ok.IsSuccess, Is.True);
        Assert.That(error.GetError().Message, Is.EqualTo("broken"));
        Assert.That(empty.GetError().Message, Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public async Task DiscardValueOnATask()
    {
        Result ok = await Task.FromResult(Result<int>.Ok(7)).DiscardValue();
        Result empty = await Task.FromResult(Empty).DiscardValue();
        Result faulted = await Task.FromException<Result<int>>(new TimeoutException()).DiscardValue();

        Assert.That(ok.IsSuccess, Is.True);
        Assert.That(empty.GetError().Message, Does.Contain("holds neither a value nor an error"));
        Assert.That(faulted.GetError(), Is.TypeOf<TimeoutException>());
    }
}
