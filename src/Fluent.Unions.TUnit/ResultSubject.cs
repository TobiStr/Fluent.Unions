using Fluent.Unions.Abstractions;

namespace Fluent.Unions.TUnit;

/// <summary>
/// A custom result type with a value, wrapped so that <c>Assert.That</c> can infer its value and error type.
/// </summary>
public readonly record struct ResultSubject<TSelf, T, TError>(TSelf Result)
    where TSelf : struct, IFluentResult<TSelf, T, TError>
    where TError : notnull;

/// <summary>
/// A custom result type without a value, wrapped so that <c>Assert.That</c> can infer its error type.
/// </summary>
public readonly record struct UnitResultSubject<TSelf, TError>(TSelf Result)
    where TSelf : struct, IFluentUnitResult<TSelf, TError>
    where TError : notnull;
