using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

/// <summary>
/// The members of generated result types that do more than store a case.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ResultMemberCore
{
    /// <summary>
    /// The error of an empty result: <c>default</c>, or created from <c>null</c>.
    /// </summary>
    public static InvalidOperationException Uninitialized(Type resultType) => ResultErrors.Uninitialized(resultType);

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The result holds an error. An error that is an exception becomes the inner exception.</exception>
    public static T GetValue<TSelf, T, TError>(TSelf result)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        if (result.TryGetValue(out var value))
            return value;

        var error = Flow.ErrorOf<TSelf, T, TError>(result);
        var exception = Flow.AsException(error);
        throw new InvalidOperationException(
            $"{ResultErrors.Name(typeof(TSelf))} holds an error, not a value: {exception?.Message ?? error.ToString()}",
            exception);
    }

    /// <summary>
    /// Gets the error.
    /// </summary>
    /// <exception cref="InvalidOperationException">The result holds a value.</exception>
    public static TError GetError<TSelf, T, TError>(TSelf result)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull =>
        result.TryGetError(out var error)
            ? error
            : throw new InvalidOperationException($"{ResultErrors.Name(typeof(TSelf))} holds a value, not an error.");

    /// <summary>
    /// Gets the error.
    /// </summary>
    /// <exception cref="InvalidOperationException">The result is successful.</exception>
    public static TError GetError<TSelf, TError>(TSelf result)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull =>
        result.TryGetError(out var error)
            ? error
            : throw new InvalidOperationException($"The {ResultErrors.Name(typeof(TSelf))} is successful and holds no error.");

    /// <summary>
    /// A successful result.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public static TSelf Ok<TSelf, T, TError>(T value)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(value);

        return TSelf.FromValue(value);
    }

    /// <summary>
    /// A failed result.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <c>null</c>.</exception>
    public static TSelf Error<TSelf, TError>(TError error)
        where TSelf : struct, IErrorResult<TSelf, TError>
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(error);

        return TSelf.FromError(error);
    }

    /// <summary>
    /// Splits the result Go style: <c>var (value, error) = result;</c>. Exactly one of them is set.
    /// </summary>
    public static void Deconstruct<TSelf, T, TError>(TSelf result, out T? value, [MaybeNull] out TError error)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        result.TryGetValue(out value);
        result.TryGetError(out error);
    }
}
