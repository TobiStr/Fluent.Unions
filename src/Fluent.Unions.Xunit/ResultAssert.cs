using Assert = global::Xunit.Assert;

namespace Fluent.Unions.Xunit;

/// <summary>
/// xUnit assertions for <see cref="Result"/> and <see cref="Result{T}"/>.
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

    private static string Describe(Exception error) => $"{error.GetType().Name}: {error.Message}";
}
