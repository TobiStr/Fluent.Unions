namespace Fluent.Unions.Tests;

public class SequenceAndValidationTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public void CombineOptionsNeedsEveryValue()
    {
        Option<int>[] all = [1, 2, 3];
        Option<int>[] oneMissing = [1, Option.None, 3];
        Option<int>[] uninitialized = [1, default];

        Assert.That(all.Combine().GetValue(), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(oneMissing.Combine().Value, Is.TypeOf<None>());
        Assert.That(uninitialized.Combine().Value, Is.TypeOf<None>());
        Assert.That(Array.Empty<Option<int>>().Combine().GetValue(), Is.Empty);
    }

    [Test]
    public void PartitionSplitsValuesAndErrors()
    {
        var other = new ArgumentException("Other");
        Result<int>[] results = [1, Failure, default, 4, other];

        var (values, errors) = results.Partition();

        Assert.That(values, Is.EqualTo(new[] { 1, 4 }));
        Assert.That(errors, Is.EqualTo(new Exception[] { Failure, other }));
    }

    [Test]
    public void CombineAllCollectsEveryError()
    {
        var other = new ArgumentException("Other");
        Result<int>[] ok = [1, default, 2];
        Result<int>[] failed = [1, Failure, 2, other];

        Assert.That(ok.CombineAll().GetOk(), Is.EqualTo(new[] { 1, 2 }));
        var aggregate = failed.CombineAll().GetError() as AggregateException;
        Assert.That(aggregate!.InnerExceptions, Is.EqualTo(new Exception[] { Failure, other }));
    }

    [Test]
    public void CombineStillStopsAtTheFirstError()
    {
        Result<int>[] failed = [1, Failure, new ArgumentException("Other")];

        Assert.That(failed.Combine().GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void ValidateCollectsEveryFailedRule()
    {
        Result<string> name = "x";
        var tooShort = new ArgumentException("Too short");
        var notUpper = new ArgumentException("Not upper case");

        var result = name.Validate(
            n => n.Length >= 3 ? Result.Ok : tooShort,
            n => char.IsUpper(n[0]) ? Result.Ok : notUpper,
            n => n.Length < 10 ? Result.Ok : new ArgumentException("Too long"));

        var aggregate = (AggregateException)result.GetError();
        Assert.That(aggregate.InnerExceptions, Is.EqualTo(new Exception[] { tooShort, notUpper }));
    }

    [Test]
    public void ValidateKeepsAValidValueAndPassesErrorsThrough()
    {
        var calls = 0;
        Result<string> valid = "Ada";
        Result<string> error = Failure;

        var kept = valid.Validate(n => n.Length >= 3 ? Result.Ok : new ArgumentException());
        var skipped = error.Validate(_ =>
        {
            calls++;
            return Result.Ok;
        });

        Assert.That(kept.GetOk(), Is.EqualTo("Ada"));
        Assert.That(skipped.GetError(), Is.SameAs(Failure));
        Assert.That(default(Result<string>).Validate(_ => Result.Ok).Value, Is.Null);
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void ValidateTreatsThrowingAndEmptyRulesAsFailuresAndFlattens()
    {
        Result<int> value = 1;
        var inner = new ArgumentException("Inner");

        var result = value.Validate(
            _ => throw Failure,
            _ => default,
            _ => new AggregateException(inner));

        var errors = ((AggregateException)result.GetError()).InnerExceptions;
        Assert.That(errors[0], Is.SameAs(Failure));
        Assert.That(errors[1], Is.TypeOf<InvalidOperationException>());
        Assert.That(errors[2], Is.SameAs(inner));
    }

    [Test]
    public void ValidateRejectsNullRules()
    {
        Result<int> value = 1;

        Assert.Throws<ArgumentNullException>(() => value.Validate(null!));
        Assert.Throws<ArgumentNullException>(() => value.Validate(_ => Result.Ok, null!));
    }

    [Test]
    public async Task ValidateOnTask()
    {
        var result = await Task.FromResult<Result<int>>(5).Validate(v => v > 10 ? Result.Ok : Failure);

        Assert.That(((AggregateException)result.GetError()).InnerExceptions.Single(), Is.SameAs(Failure));
    }
}
