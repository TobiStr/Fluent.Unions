using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace Fluent.Unions.TUnit;

/// <summary>
/// TUnit assertions for <see cref="Result"/>, <see cref="Result{T}"/> and <see cref="Option{T}"/>.
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

    private static string Describe(Exception error) => $"{error.GetType().Name}: {error.Message}";
}
