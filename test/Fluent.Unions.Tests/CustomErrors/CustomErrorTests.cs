using Fluent.Unions.Abstractions;

namespace CustomErrorSample;

public class CustomErrorTests
{
    private static readonly NotFoundException NotFound = new("Not found");

    [Test]
    public void ChainReturnsTheCustomType()
    {
        Result<int> length = GetName("Ada").Chain(GetLength);
        Result<int> fromLambda = GetName("Ada").Chain(Result<int> (name) => name.Length * 2);
        Result<int> fromCast = GetName("Ada").Chain(name => (Result<int>)name.Length);

        Assert.That(length.Value, Is.EqualTo(3));
        Assert.That(fromLambda.Value, Is.EqualTo(6));
        Assert.That(fromCast.Value, Is.EqualTo(3));
    }

    [Test]
    public void ChainPassesErrorAndEmptyThrough()
    {
        var calls = 0;

        var error = GetName(null).Chain(name => Count(ref calls, name));
        var empty = default(Result<string>).Chain(name => Count(ref calls, name));

        Assert.That(error.Value, Is.SameAs(NotFound));
        Assert.That(empty.Value, Is.Null);
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void ThrownDomainExceptionIsKept()
    {
        var result = GetName("Ada").Chain(Result<int> (_) => throw NotFound);

        Assert.That(result.Value, Is.SameAs(NotFound));
    }

    [Test]
    public void ThrownOtherExceptionIsWrappedByFromException()
    {
        var failure = new InvalidOperationException("Boom");

        var result = GetName("Ada").Chain(Result<int> (_) => throw failure);

        Assert.That(result.Value, Is.TypeOf<UnexpectedDomainException>());
        Assert.That(((Exception)result.Value!).InnerException, Is.SameAs(failure));
    }

    [Test]
    public void FromExceptionReturningNullThrows()
    {
        NullHookResult<int> start = 1;
        var failure = new InvalidOperationException("Boom");

        var ex = Assert.Throws<InvalidOperationException>(() => start.Do(_ => throw failure));

        Assert.That(ex!.InnerException, Is.SameAs(failure));
    }

    [Test]
    public void FromExceptionThrowingPropagates()
    {
        ThrowingHookResult<int> start = 1;

        Assert.Throws<NotSupportedException>(() => start.Do(_ => throw new InvalidOperationException()));
    }

    [Test]
    public void CatchRecoversReplacesOrFallsBack()
    {
        var recovered = GetName(null).Catch(_ => "Anonymous");
        var replaced = GetName(null).Catch(e => new DomainException("Lookup failed", e));
        var typed = GetName(null).Catch((NotFoundException _) => "Typed");
        var fallback = GetName(null).Catch("Fallback");
        var untouched = GetName("Ada").Catch(_ => "Anonymous");

        Assert.That(recovered.Value, Is.EqualTo("Anonymous"));
        Assert.That(((Exception)replaced.Value!).InnerException, Is.SameAs(NotFound));
        Assert.That(typed.Value, Is.EqualTo("Typed"));
        Assert.That(fallback.Value, Is.EqualTo("Fallback"));
        Assert.That(untouched.Value, Is.EqualTo("Ada"));
    }

    [Test]
    public void TypedCatchSkipsOtherErrors()
    {
        Result<string> other = new DomainException("Other");

        var result = other.Catch((NotFoundException _) => "Typed");

        Assert.That(result.Value, Is.TypeOf<DomainException>());
    }

    [Test]
    public void SideEffectsRunInTheirStates()
    {
        var log = new List<string>();

        GetName("Ada")
            .Do(name => log.Add($"next {name}"))
            .Do(name => log.Add($"both {name}"), e => log.Add($"both {e.Message}"))
            .OnErrorDo(e => log.Add($"error {e.Message}"))
            .Finally(() => log.Add("finally"));
        GetName(null)
            .Do(name => log.Add($"next {name}"))
            .Do(name => log.Add($"both {name}"), e => log.Add($"both {e.Message}"))
            .OnErrorDo(e => log.Add($"error {e.Message}"))
            .Finally(() => log.Add("finally"));

        Assert.That(log, Is.EqualTo(new[]
        {
            "next Ada", "both Ada", "finally",
            "both Not found", "error Not found", "finally"
        }));
    }

    [Test]
    public void WhereAndDefaultIfEmpty()
    {
        var kept = GetName("Ada").Where(name => name.Length == 3);
        var dropped = GetName("Ada").Where(name => name.Length > 3);
        var filled = dropped.DefaultIfEmpty("Default");

        Assert.That(kept.Value, Is.EqualTo("Ada"));
        Assert.That(dropped.Value, Is.Null);
        Assert.That(filled.Value, Is.EqualTo("Default"));
    }

    [Test]
    public void GetValueOrDefault()
    {
        Assert.That(GetName("Ada").GetValueOrDefault(), Is.EqualTo("Ada"));
        Assert.That(GetName(null).GetValueOrDefault(), Is.Null);
        Assert.That(GetName(null).GetValueOrDefault("Fallback"), Is.EqualTo("Fallback"));
        Assert.That(default(Result<string>).GetValueOrDefault("Fallback"), Is.EqualTo("Fallback"));
    }

    [Test]
    public void CheckReturnsValueRethrowsErrorAndRejectsEmpty()
    {
        Assert.That(GetName("Ada").Check().Value, Is.EqualTo("Ada"));
        Assert.That(Assert.Throws<NotFoundException>(() => GetName(null).Check()), Is.SameAs(NotFound));
        Assert.Throws<InvalidOperationException>(() => default(Result<string>).Check());
    }

    [Test]
    public async Task ChainAwaitChainsAsyncFunctions()
    {
        var length = await GetName("Ada").ChainAwait(GetLengthAsync);
        var failed = await GetName(null).ChainAwait(GetLengthAsync);
        var thrown = await GetName("Ada").ChainAwait(ThrowAsync);

        Assert.That(length.Value, Is.EqualTo(3));
        Assert.That(failed.Value, Is.SameAs(NotFound));
        Assert.That(thrown.Value, Is.TypeOf<UnexpectedDomainException>());
    }

    [Test]
    public async Task TaskWrapperKeepsThePipelineOneExpression()
    {
        var length = await GetNameAsync("Ada").Chain(GetLength);
        var failed = await GetNameAsync(null).Chain(GetLength);

        Assert.That(length.Value, Is.EqualTo(3));
        Assert.That(failed.Value, Is.SameAs(NotFound));
    }

    [Test]
    public void SelectWrapperProjectsAndEnablesQuerySyntax()
    {
        var length = GetName("Ada").Select(name => name.Length);
        var empty = GetName("Ada").Select(_ => (string?)null);
        var failed = GetName(null).Select(name => name.Length);
        var query = from name in GetName("Ada") select name.ToUpperInvariant();

        Assert.That(length.Value, Is.EqualTo(3));
        Assert.That(empty.Value, Is.Null);
        Assert.That(failed.Value, Is.SameAs(NotFound));
        Assert.That(query.Value, Is.EqualTo("ADA"));
    }

    [Test]
    public void ToDomainCrossesFromTheLibraryResult()
    {
        var ok = Fluent.Unions.Result.Try(() => int.Parse("42")).ToDomain();
        var wrapped = Fluent.Unions.Result.Try(() => int.Parse("x")).ToDomain();
        Fluent.Unions.Result<int> domainError = NotFound;
        var kept = domainError.ToDomain();

        Assert.That(ok.Value, Is.EqualTo(42));
        Assert.That(wrapped.Value, Is.TypeOf<UnexpectedDomainException>());
        Assert.That(((Exception)wrapped.Value!).InnerException, Is.TypeOf<FormatException>());
        Assert.That(kept.Value, Is.SameAs(NotFound));
    }

    [Test]
    public void MapErrorReturningNullBecomesAnError()
    {
        Fluent.Unions.Result<int> error = new FormatException();

        var result = error.MapError<Fluent.Unions.Result<int>, int, Exception, Result<int>, DomainException>(_ => null!);

        Assert.That(result.Value, Is.TypeOf<UnexpectedDomainException>());
    }

    [Test]
    public void MatchCoversEveryState()
    {
        Assert.That(GetName("Ada").Match(n => n, e => e.Message, () => "empty"), Is.EqualTo("Ada"));
        Assert.That(GetName(null).Match(n => n, e => e.Message, () => "empty"), Is.EqualTo("Not found"));
        Assert.That(default(Result<string>).Match(n => n, e => e.Message, () => "empty"), Is.EqualTo("empty"));
    }

    [Test]
    public void NullArgumentsThrow()
    {
        Assert.Throws<ArgumentNullException>(() => GetName("Ada").Chain((Func<string, Result<int>>)null!));
        Assert.Throws<ArgumentNullException>(() => ((IResult<Result<int>, int, DomainException>)null!).Do(_ => { }));
    }

    private static Result<string> GetName(string? name) => name is null ? NotFound : name;

    private static async Task<Result<string>> GetNameAsync(string? name)
    {
        await Task.Yield();
        return GetName(name);
    }

    private static Result<int> GetLength(string name) => name.Length;

    private static Result<int> Count(ref int calls, string name)
    {
        calls++;
        return name.Length;
    }

    private static async Task<Result<int>> GetLengthAsync(string name)
    {
        await Task.Yield();
        return name.Length;
    }

    private static async Task<Result<int>> ThrowAsync(string name)
    {
        await Task.Yield();
        throw new InvalidOperationException(name);
    }
}
