using AwesomeAssertions;
using AwesomeAssertions.Execution;

namespace Fluent.Unions.AwesomeAssertions;

/// <summary>
/// Entry points that start assertions on a <see cref="Result"/>, <see cref="Result{T}"/> or <see cref="Option{T}"/>.
/// </summary>
public static class AssertionExtensions
{
    /// <summary>
    /// Starts assertions on a <see cref="Result"/>.
    /// </summary>
    [CustomAssertion]
    public static ResultAssertions Should(this Result subject)
    {
        return new ResultAssertions(subject, AssertionChain.GetOrCreate());
    }

    /// <summary>
    /// Starts assertions on a <see cref="Result{T}"/>.
    /// </summary>
    [CustomAssertion]
    public static ResultAssertions<T> Should<T>(this Result<T> subject)
    {
        return new ResultAssertions<T>(subject, AssertionChain.GetOrCreate());
    }

    /// <summary>
    /// Starts assertions on an <see cref="Option{T}"/>.
    /// </summary>
    [CustomAssertion]
    public static OptionAssertions<T> Should<T>(this Option<T> subject)
    {
        return new OptionAssertions<T>(subject, AssertionChain.GetOrCreate());
    }
}
