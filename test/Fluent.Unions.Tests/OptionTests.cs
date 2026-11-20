namespace Fluent.Unions.Tests;

public class OptionTests
{
    private static readonly InvalidOperationException Failure = new("Failure");

    [Test]
    public void CreatesOptionsWithAndWithoutValue()
    {
        Option<int> implicitValue = 5;
        Option<int> explicitNone = Option.None;
        Option<int> defaultNone = default;
        var some = Option.Some("Ada");

        Assert.True(implicitValue.HasValue);
        Assert.False(explicitNone.HasValue);
        Assert.False(defaultNone.HasValue);
        Assert.That(some.GetValue(), Is.EqualTo("Ada"));
        Assert.Throws<ArgumentNullException>(() => Option.Some<string>(null!));
    }

    [Test]
    public void ZeroIsAValue()
    {
        Option<int> zero = 0;

        Assert.True(zero.HasValue);
        Assert.That(zero.GetValue(), Is.EqualTo(0));
    }

    [Test]
    public void NullReferenceGivesNoValue()
    {
        Option<string> none = (string)null!;

        Assert.False(none.HasValue);
    }

    [Test]
    public void TryGetValueAndGetValue()
    {
        Option<int> some = 5;
        Option<int> none = Option.None;

        Assert.True(some.TryGetValue(out var value));
        Assert.That(value, Is.EqualTo(5));
        Assert.False(none.TryGetValue(out _));
        Assert.Throws<InvalidOperationException>(() => none.GetValue());
        Assert.Throws<InvalidOperationException>(() => default(Option<int>).GetValue());
    }

    [Test]
    public void ToStringShowsValueOrNothing()
    {
        Assert.That(((Option<int>)5).ToString(), Is.EqualTo("5"));
        Assert.That(((Option<int>)Option.None).ToString(), Is.EqualTo(""));
        Assert.That(default(Option<int>).ToString(), Is.EqualTo(""));
    }

    [Test]
    public void PatternMatchingCoversNoneAndDefault()
    {
        static string Describe(Option<int> option) => option switch
        {
            int value => $"value {value}",
            None or null => "none"
        };

        Assert.That(Describe(5), Is.EqualTo("value 5"));
        Assert.That(Describe(Option.None), Is.EqualTo("none"));
        Assert.That(Describe(default), Is.EqualTo("none"));
    }

    [Test]
    public async Task OperatorsReturnARealNone()
    {
        Option<string> name = "Ada";
        Option<string> uninitialized = default;
        Result<int> error = Failure;
        string? missing = null;

        Option<int>[] produced =
        [
            name.Select(_ => (int?)null).Select(n => n ?? 0).Where(_ => false),
            name.Chain(_ => default(Option<int>)),
            uninitialized.Select(n => n.Length),
            uninitialized.Do(_ => { }).Select(n => n.Length),
            missing.ToOption().Select(n => n.Length),
            error.ToOption(),
            Array.Empty<int>().FirstOrNone(),
            name.Zip(default(Option<int>), (n, i) => i),
            await name.ChainAwait(_ => Task.FromResult(default(Option<int>)))
        ];

        Assert.That(produced.Select(option => option.Value), Has.All.TypeOf<None>());
        Assert.That(produced.Select(option => option switch { int => "value", None => "none" }), Has.All.EqualTo("none"));
    }

    [Test]
    public void SelectProjectsAndKeepsNone()
    {
        Option<string> name = "Ada";

        Assert.That(name.Select(n => n.Length).GetValue(), Is.EqualTo(3));
        Assert.That(name.Select(_ => 0).GetValue(), Is.EqualTo(0));
        Assert.False(name.Select(_ => (string?)null).HasValue);
        Assert.False(default(Option<string>).Select(n => n.Length).HasValue);
        Assert.False(((Option<string>)Option.None).Select(n => n.Length).HasValue);
    }

    [Test]
    public void ChainAndSelectManyRunOnlyWithValue()
    {
        var calls = 0;
        Option<string> some = "42";
        Option<string> none = Option.None;

        var parsed = some.Chain(Parse);
        var skipped = none.SelectMany(text =>
        {
            calls++;
            return Parse(text);
        });
        var invalid = ((Option<string>)"x").Chain(Parse);

        Assert.That(parsed.GetValue(), Is.EqualTo(42));
        Assert.False(skipped.HasValue);
        Assert.False(invalid.HasValue);
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void QuerySyntaxCombinesOptions()
    {
        var sum =
            from a in Parse("20")
            from b in Parse("22")
            where a < b
            select a + b;
        var missing =
            from a in Parse("20")
            from b in Parse("x")
            select a + b;

        Assert.That(sum.GetValue(), Is.EqualTo(42));
        Assert.False(missing.HasValue);
    }

    [Test]
    public void WhereFiltersIntoNone()
    {
        Option<int> five = 5;

        Assert.That(five.Where(v => v > 3).GetValue(), Is.EqualTo(5));
        Assert.False(five.Where(v => v > 10).HasValue);
    }

    [Test]
    public void ZipNeedsBothValues()
    {
        Option<int> a = 1;
        Option<string> b = "b";

        Assert.That(a.Zip(b).GetValue(), Is.EqualTo((1, "b")));
        Assert.That(a.Zip(b, (x, y) => $"{x}{y}").GetValue(), Is.EqualTo("1b"));
        Assert.False(a.Zip((Option<string>)Option.None).HasValue);
    }

    [Test]
    public void GetValueOrDefaultAndDefaultIfEmpty()
    {
        Option<int> some = 5;
        Option<int> none = Option.None;

        Assert.That(some.GetValueOrDefault(), Is.EqualTo(5));
        Assert.That(none.GetValueOrDefault(), Is.EqualTo(0));
        Assert.That(none.GetValueOrDefault(7), Is.EqualTo(7));
        Assert.That(default(Option<int>).GetValueOrDefault(7), Is.EqualTo(7));
        Assert.That(none.DefaultIfEmpty(9).GetValue(), Is.EqualTo(9));
        Assert.That(some.DefaultIfEmpty(9).GetValue(), Is.EqualTo(5));
    }

    [Test]
    public void SideEffectsRunInTheirStates()
    {
        var log = new List<string>();

        ((Option<int>)5)
            .Do(v => log.Add($"next {v}"))
            .Do(v => log.Add($"both {v}"), () => log.Add("both none"))
            .OnNoneDo(() => log.Add("none"))
            .Finally(() => log.Add("finally"));
        default(Option<int>)
            .Do(v => log.Add($"next {v}"))
            .Do(v => log.Add($"both {v}"), () => log.Add("both none"))
            .OnNoneDo(() => log.Add("none"))
            .Finally(() => log.Add("finally"));

        Assert.That(log, Is.EqualTo(new[] { "next 5", "both 5", "finally", "both none", "none", "finally" }));
    }

    [Test]
    public void ExceptionsInLambdasPropagate()
    {
        Option<int> some = 5;

        Assert.Throws<InvalidOperationException>(() => some.Select<int, int>(_ => throw Failure));
        Assert.Throws<InvalidOperationException>(() => some.Do(_ => throw Failure));
    }

    [Test]
    public void NullLambdasThrow()
    {
        Option<int> some = 5;

        Assert.Throws<ArgumentNullException>(() => some.Select((Func<int, int>)null!));
        Assert.Throws<ArgumentNullException>(() => some.Where(null!));
        Assert.Throws<ArgumentNullException>(() => some.OnNoneDo(null!));
    }

    [Test]
    public void ToOptionFromNullableValues()
    {
        string? name = "Ada";
        string? missing = null;
        int? number = 5;
        int? noNumber = null;

        Assert.That(name.ToOption().GetValue(), Is.EqualTo("Ada"));
        Assert.False(missing.ToOption().HasValue);
        Assert.That(number.ToOption().GetValue(), Is.EqualTo(5));
        Assert.False(noNumber.ToOption().HasValue);
    }

    [Test]
    public void ConvertsBetweenResultAndOption()
    {
        Result<int> ok = 5;
        Result<int> error = Failure;

        Assert.That(ok.ToOption().GetValue(), Is.EqualTo(5));
        Assert.False(error.ToOption().HasValue);
        Assert.False(default(Result<int>).ToOption().HasValue);

        Assert.That(((Option<int>)5).ToResult(() => Failure).GetValue(), Is.EqualTo(5));
        Assert.That(default(Option<int>).ToResult(() => Failure).GetError(), Is.SameAs(Failure));
        Assert.That(default(Option<int>).ToResult(() => throw Failure).GetError(), Is.SameAs(Failure));
        Assert.That(default(Option<int>).ToResult(() => null!).GetError(), Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void NullableToResult()
    {
        string? name = "Ada";
        string? missingName = null;
        int? age = 36;
        int? missingAge = null;

        Assert.That(name.ToResult(() => Failure).GetValue(), Is.EqualTo("Ada"));
        Assert.That(missingName.ToResult(() => Failure).GetError(), Is.SameAs(Failure));
        Assert.That(age.ToResult(() => Failure).GetValue(), Is.EqualTo(36));
        Assert.That(missingAge.ToResult(() => Failure).GetError(), Is.SameAs(Failure));
        Assert.That(missingName.ToResult(() => throw Failure).GetError(), Is.SameAs(Failure));
        Assert.That(missingAge.ToResult(() => null!).GetError(), Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void FirstOrNoneAndLastOrNone()
    {
        int[] numbers = [0, 1, 2, 3];
        int[] empty = [];

        Assert.That(numbers.FirstOrNone().GetValue(), Is.EqualTo(0));
        Assert.That(numbers.FirstOrNone(n => n > 1).GetValue(), Is.EqualTo(2));
        Assert.That(numbers.LastOrNone().GetValue(), Is.EqualTo(3));
        Assert.That(numbers.LastOrNone(n => n < 2).GetValue(), Is.EqualTo(1));
        Assert.False(empty.FirstOrNone().HasValue);
        Assert.False(numbers.FirstOrNone(n => n > 10).HasValue);
        Assert.False(empty.LastOrNone().HasValue);
    }

    [Test]
    public void ValuesSkipsOptionsWithoutValue()
    {
        Option<int>[] options = [1, Option.None, default, 4];

        Assert.That(options.Values(), Is.EqualTo(new[] { 1, 4 }));
    }

    private static Option<int> Parse(string text) => int.TryParse(text, out var value) ? value : Option.None;
}
