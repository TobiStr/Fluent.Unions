using System.Runtime.CompilerServices;
using Fluent.Unions.Abstractions;
using Assert = global::Xunit.Assert;

namespace Fluent.Unions.Xunit;

/// <summary>
/// xUnit assertions for <see cref="Result"/>, <see cref="Result{T}"/> and custom result types.
/// </summary>
public static class ResultAssert
{
    /// <summary>
    /// Asserts that the result is Ok.
    /// </summary>
    public static void Ok(Result result)
    {
        if (result.TryGetError(out var error))
        {
            Assert.Fail($"Expected an ok result, but it failed with {Describe(error)}");
        }
    }

    /// <summary>
    /// Asserts that the result is Ok and returns its value.
    /// </summary>
    public static T Ok<T>(Result<T> result)
    {
        if (result.TryGetError(out var error))
        {
            Assert.Fail($"Expected an ok result, but it failed with {Describe(error)}");
        }

        return result.GetValue();
    }

    /// <summary>
    /// Asserts that the result is an error and returns it.
    /// </summary>
    public static Exception Error(Result result)
    {
        if (!result.TryGetError(out var error))
        {
            Assert.Fail("Expected an error result, but it was ok");
        }

        return error;
    }

    /// <summary>
    /// Asserts that the result is an error and returns it.
    /// </summary>
    public static Exception Error<T>(Result<T> result)
    {
        if (!result.TryGetError(out var error))
        {
            Assert.Fail($"Expected an error result, but it was ok with value {result.GetValue()}");
        }

        return error;
    }

    /// <summary>
    /// Asserts that a custom result type with a value is Ok and returns its value.
    /// </summary>
    public static T Ok<TSelf, T, TError>(IFluentResult<TSelf, T, TError> result)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        if (result.TryGetError(out var error))
        {
            Assert.Fail($"Expected an ok result, but it failed with {Describe(error)}");
        }

        return result.TryGetValue(out var value) ? value : default!;
    }

    /// <summary>
    /// Asserts that a custom result type without a value is Ok.
    /// </summary>
    public static void Ok<TSelf, TError>(IFluentUnitResult<TSelf, TError> result)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        if (result.TryGetError(out var error))
        {
            Assert.Fail($"Expected an ok result, but it failed with {Describe(error)}");
        }
    }

    /// <summary>
    /// Asserts that a custom result type with a value is an error and returns it.
    /// </summary>
    public static TError Error<TSelf, T, TError>(IFluentResult<TSelf, T, TError> result)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        if (!result.TryGetError(out var error))
        {
            Assert.Fail($"Expected an error result, but it was ok with value {(result.TryGetValue(out var value) ? value : default)}");
        }

        return error;
    }

    /// <summary>
    /// Asserts that a custom result type without a value is an error and returns it.
    /// </summary>
    public static TError Error<TSelf, TError>(IFluentUnitResult<TSelf, TError> result)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        if (!result.TryGetError(out var error))
        {
            Assert.Fail("Expected an error result, but it was ok");
        }

        return error;
    }

    /// <summary>
    /// Asserts that the result is an error of type <typeparamref name="TException"/> or a derived type and returns it.
    /// A <see cref="Result{T}"/> converts to <see cref="Result"/> implicitly.
    /// </summary>
    public static TException Error<TException>(Result result)
        where TException : Exception
    {
        if (!result.TryGetError(out var error))
        {
            Assert.Fail($"Expected an error of type {typeof(TException).Name}, but the result was ok");
        }

        var typed = error as TException;

        if (typed is null)
        {
            Assert.Fail($"Expected an error of type {typeof(TException).Name}, but it failed with {Describe(error)}");
        }

        return typed;
    }

    /// <summary>
    /// Asserts that a custom result type is an error of type <typeparamref name="TCase"/> and returns it. For an error union, <typeparamref name="TCase"/> may be a case of the union.
    /// </summary>
    public static TCase Error<TCase>(IUnion result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!ResultTypes.IsResult(result.GetType()))
        {
            Assert.Fail($"Expected a result, but it was {result.GetType().Name}");
        }

        if (((IErrorResult)result).IsSuccess)
        {
            Assert.Fail($"Expected an error of type {typeof(TCase).Name}, but the result was ok");
        }

        var error = ((IErrorResult)result).Error!;

        switch (error)
        {
            case TCase direct:
                return direct;
            case IUnion { Value: TCase inner }:
                return inner;
        }

        Assert.Fail($"Expected an error of type {typeof(TCase).Name}, but it failed with {Describe(error)}");
        return default!;
    }

    private static string Describe(object error) => error is Exception exception
        ? $"{exception.GetType().Name}: {exception.Message}"
        : $"{error.GetType().Name}: {error}";
}
