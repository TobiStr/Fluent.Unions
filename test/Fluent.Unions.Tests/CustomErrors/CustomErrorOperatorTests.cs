using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Fluent.Unions.Tests;

namespace CustomErrorSample;

public class CustomErrorOperatorTests
{
    private static readonly NotFoundException NotFound = new("Not found");

    [Test]
    public void EnsureKeepsValuePassesThroughAndFails()
    {
        var calls = 0;

        var kept = GetName("Ada").Ensure(name => name.Length == 3, _ => new DomainException("Wrong length"));
        var failed = GetName("Ada").Ensure(name => name.Length > 3, name => new DomainException($"Too short: {name}"));
        var error = GetName(null).Ensure(_ => Count(ref calls), _ => new DomainException("Unused"));
        var empty = default(Result<string>).Ensure(_ => Count(ref calls), _ => new DomainException("Unused"));

        Assert.That(kept.Value, Is.EqualTo("Ada"));
        Assert.That(((Exception)failed.Value!).Message, Is.EqualTo("Too short: Ada"));
        Assert.That(error.Value, Is.SameAs(NotFound));
        Assert.That(empty.Value, Is.Null);
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void EnsureWithMessageBecomesValidationExceptionThroughFromException()
    {
        var kept = GetName("Ada").Ensure(name => name.Length == 3, "Wrong length");
        var failed = GetName("Ada").Ensure(name => name.Length > 3, "Too short");

        Assert.That(kept.Value, Is.EqualTo("Ada"));
        Assert.That(failed.Value, Is.TypeOf<UnexpectedDomainException>());
        Assert.That(((Exception)failed.Value!).InnerException, Is.TypeOf<ValidationException>());
        Assert.That(((Exception)failed.Value!).Message, Is.EqualTo("Too short"));
    }

    [Test]
    public void EnsureCapturesThrowingCallbacks()
    {
        var failure = new InvalidOperationException("Boom");

        var predicate = GetName("Ada").Ensure(_ => throw failure, _ => new DomainException("Unused"));
        var error = GetName("Ada").Ensure(_ => false, _ => throw failure);

        Assert.That(predicate.Value, Is.TypeOf<UnexpectedDomainException>());
        Assert.That(((Exception)predicate.Value!).InnerException, Is.SameAs(failure));
        Assert.That(error.Value, Is.TypeOf<UnexpectedDomainException>());
        Assert.That(((Exception)error.Value!).InnerException, Is.SameAs(failure));
    }

    [Test]
    public void ValidateCollectsEveryError()
    {
        IReadOnlyList<DomainException>? collected = null;

        var passed = GetName("Ada").Validate(Combine, name => GetLength(name));
        var failed = GetName("Ada").Validate(
            errors =>
            {
                collected = errors;
                return Combine(errors);
            },
            _ => Fail("First"),
            GetName,
            _ => Fail("Second"));

        Assert.That(passed.Value, Is.EqualTo("Ada"));
        Assert.That(((Exception)failed.Value!).Message, Is.EqualTo("First; Second"));
        Assert.That(collected!.Select(e => e.Message), Is.EqualTo(new[] { "First", "Second" }));
    }

    [Test]
    public void ValidatePassesErrorAndEmptyThrough()
    {
        var calls = 0;

        var error = GetName(null).Validate(Combine, name => CountName(ref calls, name));
        var empty = default(Result<string>).Validate(Combine, name => CountName(ref calls, name));

        Assert.That(error.Value, Is.SameAs(NotFound));
        Assert.That(empty.Value, Is.Null);
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void ValidateCountsThrowingAndEmptyRulesAsFailures()
    {
        var failure = new InvalidOperationException("Boom");
        IReadOnlyList<DomainException>? collected = null;

        var result = GetName("Ada").Validate(
            errors =>
            {
                collected = errors;
                return Combine(errors);
            },
            Result<string> (_) => throw failure,
            _ => default(Result<string>));

        Assert.That(result.Value, Is.TypeOf<DomainException>());
        Assert.That(collected, Has.Count.EqualTo(2));
        Assert.That(collected![0], Is.TypeOf<UnexpectedDomainException>());
        Assert.That(collected[0].InnerException, Is.SameAs(failure));
        Assert.That(collected[1].InnerException, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void ValidateCapturesAThrowingCombine()
    {
        var failure = new InvalidOperationException("Boom");

        var result = GetName("Ada").Validate(_ => throw failure, _ => Fail("Rule"));

        Assert.That(result.Value, Is.TypeOf<UnexpectedDomainException>());
        Assert.That(((Exception)result.Value!).InnerException, Is.SameAs(failure));
    }

    [Test]
    public void OutCapturesOnlyAValue()
    {
        var kept = GetName("Ada").Out(out var fromValue);
        GetName(null).Out(out var fromError);
        default(Result<string>).Out(out var fromEmpty);

        Assert.That(kept.Value, Is.EqualTo("Ada"));
        Assert.That(fromValue.HasValue, Is.True);
        Assert.That(fromValue.Value, Is.EqualTo("Ada"));
        Assert.That(fromError.HasValue, Is.False);
        Assert.That(fromEmpty.HasValue, Is.False);
    }

    [Test]
    public async Task DoAwaitRunsOnlyForAValue()
    {
        var log = new List<string>();
        var failure = new InvalidOperationException("Boom");

        var value = await GetName("Ada").DoAwait(name => LogAsync(log, name));
        var error = await GetName(null).DoAwait(name => LogAsync(log, name));
        var empty = await default(Result<string>).DoAwait(name => LogAsync(log, name));
        var thrown = await GetName("Ada").DoAwait(_ => ThrowTaskAsync(failure));

        Assert.That(value.Value, Is.EqualTo("Ada"));
        Assert.That(error.Value, Is.SameAs(NotFound));
        Assert.That(empty.Value, Is.Null);
        Assert.That(log, Is.EqualTo(new[] { "Ada" }));
        Assert.That(thrown.Value, Is.TypeOf<UnexpectedDomainException>());
        Assert.That(((Exception)thrown.Value!).InnerException, Is.SameAs(failure));
    }

    [Test]
    public async Task CatchAwaitHandlesOnlyAnError()
    {
        var failure = new InvalidOperationException("Boom");

        var value = await GetName("Ada").CatchAwait(_ => RecoverAsync("Guest"));
        var recovered = await GetName(null).CatchAwait(_ => RecoverAsync("Guest"));
        var empty = await default(Result<string>).CatchAwait(_ => RecoverAsync("Guest"));
        var thrown = await GetName(null).CatchAwait(Task<Result<string>> (_) => throw failure);

        Assert.That(value.Value, Is.EqualTo("Ada"));
        Assert.That(recovered.Value, Is.EqualTo("Guest"));
        Assert.That(empty.Value, Is.Null);
        Assert.That(thrown.Value, Is.TypeOf<UnexpectedDomainException>());
        Assert.That(((Exception)thrown.Value!).InnerException, Is.SameAs(failure));
    }

    [Test]
    public async Task FinallyAwaitRunsForEveryState()
    {
        var log = new List<string>();
        var failure = new InvalidOperationException("Boom");

        var value = await GetName("Ada").FinallyAwait(() => LogAsync(log, "value"));
        var error = await GetName(null).FinallyAwait(() => LogAsync(log, "error"));
        var empty = await default(Result<string>).FinallyAwait(() => LogAsync(log, "empty"));
        var thrown = await GetName("Ada").FinallyAwait(() => ThrowTaskAsync(failure));

        Assert.That(value.Value, Is.EqualTo("Ada"));
        Assert.That(error.Value, Is.SameAs(NotFound));
        Assert.That(empty.Value, Is.Null);
        Assert.That(log, Is.EqualTo(new[] { "value", "error", "empty" }));
        Assert.That(thrown.Value, Is.TypeOf<UnexpectedDomainException>());
    }

    [Test]
    public async Task WhereAwaitFiltersAValue()
    {
        var failure = new InvalidOperationException("Boom");

        var kept = await GetName("Ada").WhereAwait(name => TestAsync(name.Length == 3));
        var dropped = await GetName("Ada").WhereAwait(name => TestAsync(name.Length > 3));
        var error = await GetName(null).WhereAwait(name => TestAsync(name.Length == 3));
        var empty = await default(Result<string>).WhereAwait(name => TestAsync(name.Length == 3));
        var thrown = await GetName("Ada").WhereAwait(Task<bool> (_) => throw failure);

        Assert.That(kept.Value, Is.EqualTo("Ada"));
        Assert.That(dropped.Value, Is.Null);
        Assert.That(error.Value, Is.SameAs(NotFound));
        Assert.That(empty.Value, Is.Null);
        Assert.That(thrown.Value, Is.TypeOf<UnexpectedDomainException>());
        Assert.That(((Exception)thrown.Value!).InnerException, Is.SameAs(failure));
    }

    [Test]
    public void ToOptionKeepsOnlyAValue()
    {
        Assert.That(GetName("Ada").ToOption().TryGetValue(out var name), Is.True);
        Assert.That(name, Is.EqualTo("Ada"));
        Assert.That(GetName(null).ToOption().HasValue, Is.False);
        Assert.That(default(Result<string>).ToOption().HasValue, Is.False);
    }

    [Test]
    public void ToLoadableMapsEveryState()
    {
        var loaded = GetName("Ada").ToLoadable();
        var failed = GetName(null).ToLoadable();
        var empty = default(Result<string>).ToLoadable();
        var wrapped = new Fluent.Unions.Result<string, CheckoutError>((CheckoutError)new CartEmpty()).ToLoadable();

        Assert.That(loaded.Value, Is.EqualTo("Ada"));
        Assert.That(failed.Value, Is.SameAs(NotFound));
        Assert.That(empty.IsNotLoaded, Is.True);
        Assert.That(wrapped.Value, Is.TypeOf<ResultErrorException>());
    }

    [Test]
    public void UnwrapFlattensAResultOfAResult()
    {
        var value = Result<Result<int>>.FromValue(5).Unwrap();
        var inner = Result<Result<int>>.FromValue(Result<int>.FromError(NotFound)).Unwrap();
        var error = Result<Result<int>>.FromError(NotFound).Unwrap();
        var empty = default(Result<Result<int>>).Unwrap();

        Assert.That(value.Value, Is.EqualTo(5));
        Assert.That(inner.Value, Is.SameAs(NotFound));
        Assert.That(error.Value, Is.SameAs(NotFound));
        Assert.That(empty.Value, Is.Null);
    }

    [Test]
    public void RecordResultSetsStatusAndRecordsTheError()
    {
        using var ok = new Activity("ok").Start();
        using var failed = new Activity("failed").Start();
        using var empty = new Activity("empty").Start();
        using var wrapped = new Activity("wrapped").Start();

        var returned = ok.RecordResult(GetName("Ada"));
        failed.RecordResult(GetName(null));
        empty.RecordResult(default(Result<string>));
        wrapped.RecordResult(new Fluent.Unions.Result<string, CheckoutError>((CheckoutError)new CartEmpty()));

        Assert.That(returned, Is.SameAs(ok));
        Assert.That(ok.Status, Is.EqualTo(ActivityStatusCode.Ok));
        Assert.That(failed.Status, Is.EqualTo(ActivityStatusCode.Error));
        Assert.That(failed.StatusDescription, Is.EqualTo("Not found"));
        Assert.That(failed.Events.Single().Name, Is.EqualTo("exception"));
        Assert.That(empty.Status, Is.EqualTo(ActivityStatusCode.Error));
        Assert.That(empty.StatusDescription, Is.EqualTo("Result is empty"));
        Assert.That(empty.Events, Is.Empty);
        Assert.That(wrapped.Status, Is.EqualTo(ActivityStatusCode.Error));
    }

    [Test]
    public async Task SelectAwaitProjectsWithExplicitTypeArguments()
    {
        var length = await GetName("Ada")
            .SelectAwait<Result<string>, string, DomainException, Result<int>, int>(name => LengthAsync(name));
        var error = await GetName(null)
            .SelectAwait<Result<string>, string, DomainException, Result<int>, int>(name => LengthAsync(name));
        var empty = await default(Result<string>)
            .SelectAwait<Result<string>, string, DomainException, Result<int>, int>(name => LengthAsync(name));
        var thrown = await GetName("Ada")
            .SelectAwait<Result<string>, string, DomainException, Result<int>, int>(
                Task<int> (_) => throw new InvalidOperationException("Boom"));

        Assert.That(length.Value, Is.EqualTo(3));
        Assert.That(error.Value, Is.SameAs(NotFound));
        Assert.That(empty.Value, Is.Null);
        Assert.That(thrown.Value, Is.TypeOf<UnexpectedDomainException>());
    }

    [Test]
    public void ZipCombinesWithExplicitTypeArguments()
    {
        var zipped = GetName("Ada").Zip<Result<string>, string, DomainException, Result<int>, int, Result<string>, string>(
            GetLength("Ada"),
            (name, length) => $"{name}:{length}");
        var firstError = GetName(null).Zip<Result<string>, string, DomainException, Result<int>, int, Result<string>, string>(
            Result<int>.FromError(new DomainException("Second")),
            (name, length) => $"{name}:{length}");
        var secondError = GetName("Ada").Zip<Result<string>, string, DomainException, Result<int>, int, Result<string>, string>(
            Result<int>.FromError(new DomainException("Second")),
            (name, length) => $"{name}:{length}");
        var empty = GetName("Ada").Zip<Result<string>, string, DomainException, Result<int>, int, Result<string>, string>(
            default(Result<int>),
            (name, length) => $"{name}:{length}");
        var thrown = GetName("Ada").Zip<Result<string>, string, DomainException, Result<int>, int, Result<string>, string>(
            GetLength("Ada"),
            (_, _) => throw new InvalidOperationException("Boom"));

        Assert.That(zipped.Value, Is.EqualTo("Ada:3"));
        Assert.That(firstError.Value, Is.SameAs(NotFound));
        Assert.That(((Exception)secondError.Value!).Message, Is.EqualTo("Second"));
        Assert.That(empty.Value, Is.Null);
        Assert.That(thrown.Value, Is.TypeOf<UnexpectedDomainException>());
    }

    [Test]
    public void BindAppendKeepsBothValuesWithExplicitTypeArguments()
    {
        var calls = 0;

        var pair = GetName("Ada").BindAppend<Result<string>, string, DomainException, Result<int>, int, Result<(string, int)>>(GetLength);
        var inner = GetName("Ada").BindAppend<Result<string>, string, DomainException, Result<int>, int, Result<(string, int)>>(
            _ => Result<int>.FromError(NotFound));
        var error = GetName(null).BindAppend<Result<string>, string, DomainException, Result<int>, int, Result<(string, int)>>(
            name => CountLength(ref calls, name));
        var thrown = GetName("Ada").BindAppend<Result<string>, string, DomainException, Result<int>, int, Result<(string, int)>>(
            Result<int> (_) => throw new InvalidOperationException("Boom"));

        Assert.That(pair.Value, Is.EqualTo(("Ada", 3)));
        Assert.That(inner.Value, Is.SameAs(NotFound));
        Assert.That(error.Value, Is.SameAs(NotFound));
        Assert.That(calls, Is.EqualTo(0));
        Assert.That(thrown.Value, Is.TypeOf<UnexpectedDomainException>());
    }

    [Test]
    public void WithValueGivesASuccessfulResultTheValue()
    {
        var value = GetName("Ada").WithValue<Result<string>, string, DomainException, Result<int>, int>(7);
        var error = GetName(null).WithValue<Result<string>, string, DomainException, Result<int>, int>(7);
        var empty = default(Result<string>).WithValue<Result<string>, string, DomainException, Result<int>, int>(7);

        Assert.That(value.Value, Is.EqualTo(7));
        Assert.That(error.Value, Is.SameAs(NotFound));
        Assert.That(empty.Value, Is.Null);
    }

    [Test]
    public void TransposeSwapsAResultOfAnOption()
    {
        var some = Result<Option<int>>.FromValue(Option.Some(5))
            .Transpose<Result<Option<int>>, int, DomainException, Result<int>>();
        var none = Result<Option<int>>.FromValue(Option.None)
            .Transpose<Result<Option<int>>, int, DomainException, Result<int>>();
        var error = Result<Option<int>>.FromError(NotFound)
            .Transpose<Result<Option<int>>, int, DomainException, Result<int>>();
        var empty = default(Result<Option<int>>)
            .Transpose<Result<Option<int>>, int, DomainException, Result<int>>();

        Assert.That(some.TryGetValue(out var kept), Is.True);
        Assert.That(kept.Value, Is.EqualTo(5));
        Assert.That(none.HasValue, Is.False);
        Assert.That(error.TryGetValue(out var failed), Is.True);
        Assert.That(failed.Value, Is.SameAs(NotFound));
        Assert.That(empty.TryGetValue(out var blank), Is.True);
        Assert.That(blank.Value, Is.Null);
    }

    [Test]
    public void NullArgumentsThrow()
    {
        Assert.Throws<ArgumentNullException>(() => GetName("Ada").Ensure(_ => true, (string)null!));
        Assert.Throws<ArgumentNullException>(() => GetName("Ada").Validate(Combine, (Func<string, Result<string>>)null!));
        Assert.Throws<ArgumentNullException>(() => GetName("Ada").Validate(null!, GetName));
        Assert.Throws<ArgumentNullException>(() => GetName("Ada").DoAwait(null!));
    }

    private static Result<string> GetName(string? name) => name is null ? NotFound : name;

    private static Result<int> GetLength(string name) => name.Length;

    private static DomainException Combine(IReadOnlyList<DomainException> errors) =>
        new(string.Join("; ", errors.Select(error => error.Message)));

    private static bool Count(ref int calls)
    {
        calls++;
        return true;
    }

    private static Result<string> CountName(ref int calls, string name)
    {
        calls++;
        return name;
    }

    private static Result<int> CountLength(ref int calls, string name)
    {
        calls++;
        return name.Length;
    }

    private static Result<string> Fail(string message) => new DomainException(message);

    private static async Task LogAsync(List<string> log, string entry)
    {
        await Task.Yield();
        log.Add(entry);
    }

    private static async Task ThrowTaskAsync(Exception exception)
    {
        await Task.Yield();
        throw exception;
    }

    private static async Task<Result<string>> RecoverAsync(string fallback)
    {
        await Task.Yield();
        return fallback;
    }

    private static async Task<bool> TestAsync(bool outcome)
    {
        await Task.Yield();
        return outcome;
    }

    private static async Task<int> LengthAsync(string name)
    {
        await Task.Yield();
        return name.Length;
    }
}
