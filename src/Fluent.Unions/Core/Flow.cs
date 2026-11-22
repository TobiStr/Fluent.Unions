using System.Diagnostics;
using System.Runtime.CompilerServices;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Core;

/// <summary>
/// Shared steps of the operator cores.
/// </summary>
internal static class Flow
{
    /// <summary>
    /// Gets the error of a result that holds no value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TError ErrorOf<TSelf, T, TError>(TSelf result)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull =>
        result.TryGetError(out var error) ? error : throw new UnreachableException();

    /// <summary>
    /// Gets the error of a result without a value that is not successful.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TError ErrorOf<TSelf, TError>(TSelf result)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull =>
        result.TryGetError(out var error) ? error : throw new UnreachableException();

    /// <summary>
    /// Indicates whether an exception thrown by user code becomes an error of <typeparamref name="TResult"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Captured<TResult, TError>(Exception exception)
        where TResult : struct, IErrorResult<TResult, TError>
        where TError : notnull =>
        exception is not OperationCanceledException && TResult.Captures(exception);

    /// <summary>
    /// Turns an exception thrown by user code into a failed result.
    /// </summary>
    internal static TResult Thrown<TResult, TError>(Exception exception)
        where TResult : struct, IErrorResult<TResult, TError>
        where TError : notnull =>
        TResult.FromError(Capture.ToError<TResult, TError>(exception));

    /// <summary>
    /// Awaits a result task. A faulted task becomes a failed result; cancellation propagates.
    /// </summary>
    internal static async ValueTask<TResult> Settle<TResult, TError>(Task<TResult> task)
        where TResult : struct, IErrorResult<TResult, TError>
        where TError : notnull
    {
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (Exception exception) when (Flow.Captured<TResult, TError>(exception))
        {
            return Thrown<TResult, TError>(exception);
        }
    }

    /// <summary>
    /// Matches the error, or a case inside an error union, against <typeparamref name="TCase"/>.
    /// </summary>
    internal static bool TryGetCase<TError, TCase>(TError error, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TCase? match)
        where TError : notnull
    {
        switch (error)
        {
            case TCase direct:
                match = direct;
                return true;
            case IUnion { Value: TCase inner }:
                match = inner;
                return true;
            default:
                match = default;
                return false;
        }
    }

    /// <summary>
    /// The error as an exception: the error itself, an exception case of an error union, or <c>null</c>.
    /// </summary>
    internal static Exception? AsException<TError>(TError error) where TError : notnull => error switch
    {
        Exception exception => exception,
        IUnion { Value: Exception inner } => inner,
        _ => null
    };

    /// <summary>
    /// Combines several errors into one: a <see cref="ValidationErrors"/> when all are validation failures, otherwise an
    /// <see cref="AggregateException"/>. Errors that are not exceptions keep the first one.
    /// </summary>
    internal static TError Combine<TResult, TError>(IReadOnlyList<TError> errors)
        where TResult : struct, IErrorResult<TResult, TError>
        where TError : notnull
    {
        var exceptions = new List<Exception>(errors.Count);
        foreach (var error in errors)
        {
            if (AsException(error) is not { } exception)
                return errors[0];
            exceptions.Add(exception);
        }

        return Capture.ToError<TResult, TError>(ValidationErrors.Combine(exceptions));
    }
}
