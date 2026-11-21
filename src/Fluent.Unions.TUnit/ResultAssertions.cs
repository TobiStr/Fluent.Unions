using Fluent.Unions.Abstractions;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace Fluent.Unions.TUnit;

/// <summary>
/// TUnit assertions for <see cref="Result"/>, <see cref="Result{T}"/>, <see cref="Option{T}"/> and custom result types.
/// </summary>
public static class ResultAssertions
{
    /// <summary>
    /// Asserts that the result is Ok.
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to be an ok result")]
    public static AssertionResult IsOk(this Result result)
    {
        return result.TryGetError(out var error)
            ? AssertionResult.Failed($"it failed with {Describe(error)}")
            : AssertionResult.Passed;
    }

    /// <summary>
    /// Asserts that the result is Ok and returns its value.
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to be an ok result")]
    public static AssertionResult<T> IsOk<T>(this Result<T> result)
    {
        return result.TryGetError(out var error)
            ? AssertionResult.Failed($"it failed with {Describe(error)}")
            : AssertionResult<T>.Passed(result.GetValue());
    }

    /// <summary>
    /// Asserts that the result is an error and returns it.
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to be an error result")]
    public static AssertionResult<Exception> IsError(this Result result)
    {
        return result.TryGetError(out var error)
            ? AssertionResult<Exception>.Passed(error)
            : AssertionResult.Failed("it was ok");
    }

    /// <summary>
    /// Asserts that the result is an error and returns it.
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to be an error result")]
    public static AssertionResult<Exception> IsError<T>(this Result<T> result)
    {
        return result.TryGetError(out var error)
            ? AssertionResult<Exception>.Passed(error)
            : AssertionResult.Failed($"it was ok with value {result.GetValue()}");
    }

    /// <summary>
    /// Asserts that the result is an error of type <typeparamref name="TException"/> or a derived type and returns it.
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to be an error of type {TException}")]
    public static AssertionResult<TException> IsError<TException>(this Result result)
        where TException : Exception
    {
        if (!result.TryGetError(out var error))
        {
            return AssertionResult.Failed("it was ok");
        }

        return error is TException typed
            ? AssertionResult<TException>.Passed(typed)
            : AssertionResult.Failed($"it failed with {Describe(error)}");
    }

    /// <summary>
    /// Lets <c>Assert.That</c> assert on a custom result type with a value. Call it as <c>Assert.That(result.AsResult()).IsOk()</c>.
    /// </summary>
    public static ResultSubject<TSelf, T, TError> AsResult<TSelf, T, TError>(this IFluentResult<TSelf, T, TError> result)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        return new ResultSubject<TSelf, T, TError>((TSelf)result);
    }

    /// <summary>
    /// Lets <c>Assert.That</c> assert on a custom result type without a value. Call it as <c>Assert.That(result.AsResult()).IsOk()</c>.
    /// </summary>
    public static UnitResultSubject<TSelf, TError> AsResult<TSelf, TError>(this IFluentUnitResult<TSelf, TError> result)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        return new UnitResultSubject<TSelf, TError>((TSelf)result);
    }

    /// <summary>
    /// Asserts that a custom result type with a value is Ok and returns its value.
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to be an ok result")]
    public static AssertionResult<T> IsOk<TSelf, T, TError>(this ResultSubject<TSelf, T, TError> subject)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        var result = subject.Result;

        if (result.TryGetError(out var error))
        {
            return AssertionResult.Failed($"it failed with {Describe(error)}");
        }

        return AssertionResult<T>.Passed(result.TryGetValue(out var value) ? value : default!);
    }

    /// <summary>
    /// Asserts that a custom result type without a value is Ok.
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to be an ok result")]
    public static AssertionResult IsOk<TSelf, TError>(this UnitResultSubject<TSelf, TError> subject)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        return subject.Result.TryGetError(out var error)
            ? AssertionResult.Failed($"it failed with {Describe(error)}")
            : AssertionResult.Passed;
    }

    /// <summary>
    /// Asserts that a custom result type with a value is an error and returns it.
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to be an error result")]
    public static AssertionResult<TError> IsError<TSelf, T, TError>(this ResultSubject<TSelf, T, TError> subject)
        where TSelf : struct, IFluentResult<TSelf, T, TError>
        where TError : notnull
    {
        var result = subject.Result;

        return result.TryGetError(out var error)
            ? AssertionResult<TError>.Passed(error)
            : AssertionResult.Failed($"it was ok with value {(result.TryGetValue(out var value) ? value : default)}");
    }

    /// <summary>
    /// Asserts that a custom result type without a value is an error and returns it.
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to be an error result")]
    public static AssertionResult<TError> IsError<TSelf, TError>(this UnitResultSubject<TSelf, TError> subject)
        where TSelf : struct, IFluentUnitResult<TSelf, TError>
        where TError : notnull
    {
        return subject.Result.TryGetError(out var error)
            ? AssertionResult<TError>.Passed(error)
            : AssertionResult.Failed("it was ok");
    }

    /// <summary>
    /// Asserts that the option has a value and returns it.
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to be an option with a value")]
    public static AssertionResult<T> IsSome<T>(this Option<T> option)
    {
        return option.TryGetValue(out var value)
            ? AssertionResult<T>.Passed(value)
            : AssertionResult.Failed("it was none");
    }

    /// <summary>
    /// Asserts that the option has no value. A default option counts as none.
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to be an option without a value")]
    public static AssertionResult IsNone<T>(this Option<T> option)
    {
        return option.TryGetValue(out var value)
            ? AssertionResult.Failed($"it was some with value {value}")
            : AssertionResult.Passed;
    }

    private static string Describe(object error) => error is Exception exception
        ? $"{exception.GetType().Name}: {exception.Message}"
        : $"{error.GetType().Name}: {error}";
}
