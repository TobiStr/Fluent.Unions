using System.Collections;
using Fluent.Unions.Validation;
using Is = Fluent.Unions.Validation.Is;

namespace Fluent.Unions.Tests;

internal sealed class CountingSequence(params int[] items) : IEnumerable<int>
{
    public int Enumerations { get; private set; }

    public IEnumerator<int> GetEnumerator()
    {
        Enumerations++;
        return ((IEnumerable<int>)items).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class IsCollectionTests
{
    [Test]
    public void NotEmptyAndEmptyOnListArrayAndSequence()
    {
        Assert.That(Is.NotEmpty(new List<int> { 1 }), NUnit.Framework.Is.True);
        Assert.That(Is.NotEmpty(new[] { 1 }), NUnit.Framework.Is.True);
        Assert.That(Is.NotEmpty(Enumerable.Range(1, 3)), NUnit.Framework.Is.True);
        Assert.That(Is.NotEmpty(new List<int>()), NUnit.Framework.Is.False);
        Assert.That(Is.NotEmpty<int>(null), NUnit.Framework.Is.False);
        Assert.That(Is.Empty(new List<int>()), NUnit.Framework.Is.True);
        Assert.That(Is.Empty(Enumerable.Empty<int>()), NUnit.Framework.Is.True);
        Assert.That(Is.Empty<int>(null), NUnit.Framework.Is.True);
        Assert.That(Is.Empty(new[] { 1 }), NUnit.Framework.Is.False);
    }

    [Test]
    public void StringKeepsItsOwnNotEmpty()
    {
        Assert.That(Is.NotEmpty(""), NUnit.Framework.Is.False);
        Assert.That(Is.NotEmpty("a"), NUnit.Framework.Is.True);
    }

    [Test]
    public void NotEmptyWorksAsAMethodGroup()
    {
        Option<List<int>> none = Option.Some(new List<int>()).Where(Is.NotEmpty);
        Option<string> text = Option.Some("").Where(Is.NotEmpty);

        Assert.That(none.HasValue, NUnit.Framework.Is.False);
        Assert.That(text.HasValue, NUnit.Framework.Is.False);
    }

    [Test]
    public void Unique()
    {
        Assert.That(Is.Unique(new[] { 1, 2, 3 }), NUnit.Framework.Is.True);
        Assert.That(Is.Unique(new[] { 1, 2, 1 }), NUnit.Framework.Is.False);
        Assert.That(Is.Unique(Array.Empty<int>()), NUnit.Framework.Is.True);
        Assert.That(Is.Unique((IEnumerable<int>?)null), NUnit.Framework.Is.False);
    }

    [Test]
    public void UniqueWithComparer()
    {
        var rule = Is.Unique(StringComparer.OrdinalIgnoreCase);

        Assert.That(rule(new[] { "a", "b" }), NUnit.Framework.Is.True);
        Assert.That(rule(new[] { "a", "A" }), NUnit.Framework.Is.False);
        Assert.That(Is.Unique(new[] { "a", "A" }), NUnit.Framework.Is.True);
    }

    [TestCase(0, 2, false)]
    [TestCase(2, 2, true)]
    [TestCase(3, 2, false)]
    public void HasCount(int items, int count, bool expected) =>
        Assert.That(Is.HasCount(count)(Enumerable.Range(0, items)), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(1, false)]
    [TestCase(2, true)]
    [TestCase(3, true)]
    public void HasMinCount(int items, bool expected) =>
        Assert.That(Is.HasMinCount(2)(Enumerable.Range(0, items)), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(1, true)]
    [TestCase(2, true)]
    [TestCase(3, false)]
    public void HasMaxCount(int items, bool expected) =>
        Assert.That(Is.HasMaxCount(2)(Enumerable.Range(0, items)), NUnit.Framework.Is.EqualTo(expected));

    [Test]
    public void CountRulesWorkOnListsArraysAndNonGenericCollections()
    {
        Assert.That(Is.HasCount(2)(new List<int> { 1, 2 }), NUnit.Framework.Is.True);
        Assert.That(Is.HasCount(2)(new[] { 1, 2 }), NUnit.Framework.Is.True);
        Assert.That(Is.HasCount(1)(new ArrayList { "a" }), NUnit.Framework.Is.True);
        Assert.That(Is.HasCount(0)(null), NUnit.Framework.Is.False);
    }

    [Test]
    public void NegativeCountThrowsWhenCreated()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Is.HasCount(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Is.HasMinCount(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Is.HasMaxCount(-1));
    }

    [Test]
    public void ContainsItemAndNotContainsItem()
    {
        Assert.That(Is.ContainsItem(2)(new[] { 1, 2 }), NUnit.Framework.Is.True);
        Assert.That(Is.ContainsItem(3)(new[] { 1, 2 }), NUnit.Framework.Is.False);
        Assert.That(Is.NotContainsItem(3)(new[] { 1, 2 }), NUnit.Framework.Is.True);
        Assert.That(Is.NotContainsItem(2)(new[] { 1, 2 }), NUnit.Framework.Is.False);
        Assert.That(Is.ContainsItem(1)(null), NUnit.Framework.Is.False);
        Assert.That(Is.NotContainsItem(1)(null), NUnit.Framework.Is.False);
    }

    [Test]
    public void AllMatchAnyMatchAndNoneMatch()
    {
        int[] items = [2, 4, 5];

        Assert.That(Is.AllMatch<int>(x => x > 1)(items), NUnit.Framework.Is.True);
        Assert.That(Is.AllMatch<int>(x => x % 2 == 0)(items), NUnit.Framework.Is.False);
        Assert.That(Is.AnyMatch<int>(x => x % 2 == 1)(items), NUnit.Framework.Is.True);
        Assert.That(Is.AnyMatch<int>(x => x > 5)(items), NUnit.Framework.Is.False);
        Assert.That(Is.NoneMatch<int>(x => x > 5)(items), NUnit.Framework.Is.True);
        Assert.That(Is.NoneMatch<int>(x => x == 4)(items), NUnit.Framework.Is.False);
    }

    [Test]
    public void EmptySequenceIsAllMatchAndNoneMatchButNotAnyMatch()
    {
        Assert.That(Is.AllMatch<int>(x => x > 1)([]), NUnit.Framework.Is.True);
        Assert.That(Is.NoneMatch<int>(x => x > 1)([]), NUnit.Framework.Is.True);
        Assert.That(Is.AnyMatch<int>(x => x > 1)([]), NUnit.Framework.Is.False);
    }

    [Test]
    public void NullSequenceFailsTheItemRules()
    {
        Assert.That(Is.AllMatch<int>(x => true)(null), NUnit.Framework.Is.False);
        Assert.That(Is.AnyMatch<int>(x => true)(null), NUnit.Framework.Is.False);
        Assert.That(Is.NoneMatch<int>(x => true)(null), NUnit.Framework.Is.False);
    }

    [Test]
    public void NullPredicateThrowsWhenCreated()
    {
        Assert.Throws<ArgumentNullException>(() => Is.AllMatch<int>(null!));
        Assert.Throws<ArgumentNullException>(() => Is.AnyMatch<int>(null!));
        Assert.Throws<ArgumentNullException>(() => Is.NoneMatch<int>(null!));
        Assert.Throws<ArgumentNullException>(() => Is.Unique<int>((IEqualityComparer<int>)null!));
    }

    [Test]
    public void EveryRuleEnumeratesALazySequenceOnce()
    {
        var sequence = new CountingSequence(1, 2, 3);
        Func<IEnumerable<int>?, bool>[] rules =
        [
            Is.NotEmpty,
            Is.Empty,
            Is.Unique,
            Is.Unique(EqualityComparer<int>.Default),
            value => Is.HasCount(3)(value),
            value => Is.HasMinCount(2)(value),
            value => Is.HasMaxCount(5)(value),
            Is.ContainsItem(2),
            Is.NotContainsItem(2),
            Is.AllMatch<int>(x => x > 0),
            Is.AnyMatch<int>(x => x > 2),
            Is.NoneMatch<int>(x => x > 5)
        ];

        foreach (var rule in rules)
        {
            var before = sequence.Enumerations;
            _ = rule(sequence);
            Assert.That(sequence.Enumerations - before, NUnit.Framework.Is.EqualTo(1));
        }
    }
}
