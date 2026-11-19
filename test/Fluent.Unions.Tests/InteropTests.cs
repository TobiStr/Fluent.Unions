using System.Globalization;

namespace Fluent.Unions.Tests;

public class InteropTests
{
    [Test]
    public void TryParseGivesValueOrNone()
    {
        Assert.That(Option.TryParse<int>("42").GetValue(), Is.EqualTo(42));
        Assert.That(Option.TryParse<int>("x").Value, Is.TypeOf<None>());
        Assert.That(Option.TryParse<int>((string?)null).Value, Is.TypeOf<None>());
        Assert.That(Option.TryParse<Guid>("not a guid").HasValue, Is.False);
        Assert.That(Option.TryParse<decimal>("1,5", CultureInfo.GetCultureInfo("de-DE")).GetValue(), Is.EqualTo(1.5m));
        Assert.That(Option.TryParse<int>("123".AsSpan(1)).GetValue(), Is.EqualTo(23));
    }

    [Test]
    public void GetValueOrNoneLooksUpDictionaries()
    {
        var ages = new Dictionary<string, int> { ["Ada"] = 36, ["Zero"] = 0 };

        Assert.That(ages.GetValueOrNone("Ada").GetValue(), Is.EqualTo(36));
        Assert.That(ages.GetValueOrNone("Zero").GetValue(), Is.EqualTo(0));
        Assert.That(ages.GetValueOrNone("Bob").Value, Is.TypeOf<None>());
    }

    [Test]
    public void SingleOrNoneFollowsSingleOrDefault()
    {
        int[] one = [5];
        int[] many = [1, 2, 3];

        Assert.That(one.SingleOrNone().GetValue(), Is.EqualTo(5));
        Assert.That(Array.Empty<int>().SingleOrNone().Value, Is.TypeOf<None>());
        Assert.That(many.SingleOrNone(n => n == 2).GetValue(), Is.EqualTo(2));
        Assert.That(many.SingleOrNone(n => n > 5).Value, Is.TypeOf<None>());
        Assert.Throws<InvalidOperationException>(() => many.SingleOrNone());
        Assert.Throws<InvalidOperationException>(() => many.SingleOrNone(n => n > 1));
    }

    [Test]
    public void ElementAtOrNoneHandlesListsSequencesAndRanges()
    {
        int[] list = [10, 20, 30];
        var sequence = Enumerable.Range(1, 3).Select(n => n * 10);

        Assert.That(list.ElementAtOrNone(1).GetValue(), Is.EqualTo(20));
        Assert.That(sequence.ElementAtOrNone(2).GetValue(), Is.EqualTo(30));
        Assert.That(list.ElementAtOrNone(3).Value, Is.TypeOf<None>());
        Assert.That(sequence.ElementAtOrNone(5).Value, Is.TypeOf<None>());
        Assert.That(list.ElementAtOrNone(-1).Value, Is.TypeOf<None>());
    }

    [Test]
    public void ToNullableConvertsValueTypes()
    {
        Option<int> some = 5;

        Assert.That(some.ToNullable(), Is.EqualTo(5));
        Assert.That(((Option<int>)Option.None).ToNullable(), Is.Null);
        Assert.That(default(Option<int>).ToNullable(), Is.Null);
    }
}
