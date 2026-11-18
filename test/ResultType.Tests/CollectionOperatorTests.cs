namespace ResultType.Tests;

public class CollectionOperatorTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public void CombineCollectsAllValues()
    {
        Result<int>[] results = [1, 2, 3];

        Assert.That(results.Combine().GetOk(), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void CombineReturnsFirstError()
    {
        var second = new InvalidOperationException("second");
        Result<int>[] results = [1, Failure, second];

        Assert.That(results.Combine().GetError(), Is.SameAs(Failure));
    }

    [Test]
    public void CombineSkipsEmptyResults()
    {
        Result<int>[] results = [1, default, 3];

        Assert.That(results.Combine().GetOk(), Is.EqualTo(new[] { 1, 3 }));
    }

    [Test]
    public void CombineSkipsValuesFilteredByWhere()
    {
        var evens = Enumerable.Range(1, 6)
            .Select(number => new Result<int>(number).Where(value => value % 2 == 0))
            .Combine();

        Assert.That(evens.GetOk(), Is.EqualTo(new[] { 2, 4, 6 }));
    }

    [Test]
    public void CombineOfEmptySequenceIsOkWithEmptyList()
    {
        var combined = Array.Empty<Result<int>>().Combine();

        Assert.True(combined.Success);
        Assert.That(combined.GetOk(), Is.Empty);
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
        Assert.That(results.Errors(), Is.EqualTo(new[] { Failure }));
    }

    [Test]
    public void LinqToListIsNotHijacked()
    {
        Result<int>[] results = [1, 2];

        var list = results.ToList();

        Assert.That(list, Is.TypeOf<List<Result<int>>>());
    }
}
