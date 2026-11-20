namespace Fluent.Unions.Tests;

public class CollectionOperatorTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public void CombineCollectsAllValues()
    {
        Result<int>[] results = [1, 2, 3];

        Assert.That(results.Combine().GetValue(), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void CombineReturnsFirstError()
    {
        var second = new InvalidOperationException("second");
        Result<int>[] results = [1, Failure, second];

        Assert.That(results.Combine().GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void CombineTreatsDefaultResultsAsErrors()
    {
        Result<int>[] results = [1, default, 3];

        Assert.That(results.Combine().GetError().Message, Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void CombineOfEmptySequenceIsOkWithEmptyList()
    {
        var combined = Array.Empty<Result<int>>().Combine();

        Assert.True(combined.IsSuccess);
        Assert.That(combined.GetValue(), Is.Empty);
    }

    [Test]
    public void CombineCapturesExceptionFromSequence()
    {
        var combined = Throwing().Combine();

        Assert.That(combined.GetError(), Is.SameAs(Failure));

        static IEnumerable<Result<int>> Throwing()
        {
            yield return 1;
            throw Failure;
        }
    }

    [Test]
    public void ValuesAndErrorsSplitSequence()
    {
        Result<int>[] results = [1, Failure, default, 4];

        Assert.That(results.Values(), Is.EqualTo(new[] { 1, 4 }));
        var errors = results.Errors().ToList();
        Assert.That(errors, Has.Count.EqualTo(2));
        Assert.That(errors[0], Is.SameAs(Failure));
        Assert.That(errors[1].Message, Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void LinqToListIsNotHijacked()
    {
        Result<int>[] results = [1, 2];

        var list = results.ToList();

        Assert.That(list, Is.TypeOf<List<Result<int>>>());
    }
}
