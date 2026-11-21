using System.Runtime.CompilerServices;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.TUnit;

/// <summary>
/// A custom result type with a value, wrapped so that <c>Assert.That</c> can infer its value and error type.
/// </summary>
public readonly record struct ResultSubject<TSelf, T, TError>(TSelf Result)
    where TSelf : struct, IFluentResult<TSelf, T, TError>
    where TError : notnull
{
    /// <summary>
    /// The error as a <typeparamref name="TCase"/>, also when it is a case of an error union, or none. Assert it with <c>IsSome()</c>.
    /// </summary>
    public Option<TCase> ErrorCase<TCase>() => Result.TryGetError(out var error) ? ErrorCases.Of<TCase>(error) : Option.None;
}

/// <summary>
/// A custom result type without a value, wrapped so that <c>Assert.That</c> can infer its error type.
/// </summary>
public readonly record struct UnitResultSubject<TSelf, TError>(TSelf Result)
    where TSelf : struct, IFluentUnitResult<TSelf, TError>
    where TError : notnull
{
    /// <summary>
    /// The error as a <typeparamref name="TCase"/>, also when it is a case of an error union, or none. Assert it with <c>IsSome()</c>.
    /// </summary>
    public Option<TCase> ErrorCase<TCase>() => Result.TryGetError(out var error) ? ErrorCases.Of<TCase>(error) : Option.None;
}

internal static class ErrorCases
{
    public static Option<TCase> Of<TCase>(object error) => error switch
    {
        TCase direct => Option.Some(direct),
        IUnion { Value: TCase inner } => Option.Some(inner),
        _ => Option.None,
    };
}
