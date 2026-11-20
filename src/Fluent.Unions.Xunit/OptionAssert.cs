using Assert = global::Xunit.Assert;

namespace Fluent.Unions.Xunit;

/// <summary>
/// xUnit assertions for <see cref="Option{T}"/>.
/// </summary>
public static class OptionAssert
{
    /// <summary>
    /// Asserts that the option has a value and returns it.
    /// </summary>
    public static T Some<T>(Option<T> option)
    {
        if (!option.TryGetValue(out var value))
        {
            Assert.Fail("Expected an option with a value, but it was none");
        }

        return value;
    }

    /// <summary>
    /// Asserts that the option has no value. A default option counts as none.
    /// </summary>
    public static void None<T>(Option<T> option)
    {
        if (option.TryGetValue(out var value))
        {
            Assert.Fail($"Expected an option without a value, but it was some with value {value}");
        }
    }
}
