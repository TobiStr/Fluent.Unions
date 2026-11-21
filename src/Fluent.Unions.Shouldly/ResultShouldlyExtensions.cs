using Fluent.Unions.Abstractions;
using Shouldly;

namespace Fluent.Unions.Shouldly;

/// <summary>
/// Shouldly assertions for <see cref="Result"/>, <see cref="Result{T}"/>, <see cref="Option{T}"/> and custom result types.
/// </summary>
public static class ResultShouldlyExtensions
{
    /// <summary>
    /// Asserts that the result is Ok.
    /// </summary>
    public static Result ShouldBeOk(this Result result, string? customMessage = null)
    {
        if (result.TryGetError(out var error))
        {
            throw Fail($"Expected an ok result, but it failed with {Describe(error)}", customMessage);
        }

        return result;
    }

    /// <summary>
    /// Asserts that the result is Ok and returns its value.
    /// </summary>
    public static T ShouldBeOk<T>(this Result<T> result, string? customMessage = null)
    {
        if (result.TryGetError(out var error))
        {
            throw Fail($"Expected an ok result, but it failed with {Describe(error)}", customMessage);
        }

        return result.GetValue();
    }

    /// <summary>
    /// Asserts that the result is an error and returns it.
    /// </summary>
    public static Exception ShouldBeError(this Result result, string? customMessage = null)
    {
        return result.TryGetError(out var error)
            ? error
            : throw Fail("Expected an error result, but it was ok", customMessage);
    }

    /// <summary>
    /// Asserts that the result is an error and returns it.
    /// </summary>
    public static Exception ShouldBeError<T>(this Result<T> result, string? customMessage = null)
    {
        return result.TryGetError(out var error)
            ? error
            : throw Fail($"Expected an error result, but it was ok with value {result.GetValue()}", customMessage);
    }

    /// <summary>
    /// Asserts that the result is an error of type <typeparamref name="TException"/> or a derived type and returns it.
    /// </summary>
    public static TException ShouldBeError<TException>(this Result result, string? customMessage = null)
        where TException : Exception
    {
        return result.TryGetError(out var error)
            ? error as TException ?? throw Fail($"Expected an error of type {typeof(TException).Name}, but it failed with {Describe(error)}", customMessage)
            : throw Fail($"Expected an error of type {typeof(TException).Name}, but the result was ok", customMessage);
    }

    /// <summary>
    /// Asserts that a custom result type with a value is Ok and returns its value.
    /// </summary>
    public static T ShouldBeOk<TSelf, T, TError>(this IFluentResult<TSelf, T, TError> result, string? customMessage = null)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        if (result.TryGetError(out var error))
        {
            throw Fail($"Expected an ok result, but it failed with {Describe(error)}", customMessage);
        }

        return result.TryGetValue(out var value) ? value : default!;
    }

    /// <summary>
    /// Asserts that a custom result type without a value is Ok.
    /// </summary>
    public static TSelf ShouldBeOk<TSelf, TError>(this IFluentUnitResult<TSelf, TError> result, string? customMessage = null)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        if (result.TryGetError(out var error))
        {
            throw Fail($"Expected an ok result, but it failed with {Describe(error)}", customMessage);
        }

        return (TSelf)result;
    }

    /// <summary>
    /// Asserts that a custom result type with a value is an error and returns it.
    /// </summary>
    public static TError ShouldBeError<TSelf, T, TError>(this IFluentResult<TSelf, T, TError> result, string? customMessage = null)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        return result.TryGetError(out var error)
            ? error
            : throw Fail($"Expected an error result, but it was ok with value {(result.TryGetValue(out var value) ? value : default)}", customMessage);
    }

    /// <summary>
    /// Asserts that a custom result type without a value is an error and returns it.
    /// </summary>
    public static TError ShouldBeError<TSelf, TError>(this IFluentUnitResult<TSelf, TError> result, string? customMessage = null)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        return result.TryGetError(out var error)
            ? error
            : throw Fail("Expected an error result, but it was ok", customMessage);
    }

    /// <summary>
    /// Asserts that the option has a value and returns it.
    /// </summary>
    public static T ShouldBeSome<T>(this Option<T> option, string? customMessage = null)
    {
        return option.TryGetValue(out var value)
            ? value
            : throw Fail("Expected an option with a value, but it was none", customMessage);
    }

    /// <summary>
    /// Asserts that the option has no value. A default option counts as none.
    /// </summary>
    public static void ShouldBeNone<T>(this Option<T> option, string? customMessage = null)
    {
        if (option.TryGetValue(out var value))
        {
            throw Fail($"Expected an option without a value, but it was some with value {value}", customMessage);
        }
    }

    private static string Describe(object error) => error is Exception exception
        ? $"{exception.GetType().Name}: {exception.Message}"
        : $"{error.GetType().Name}: {error}";

    private static ShouldAssertException Fail(string message, string? customMessage)
    {
        return new ShouldAssertException(customMessage is null ? message : $"{message}\nAdditional info: {customMessage}");
    }
}
