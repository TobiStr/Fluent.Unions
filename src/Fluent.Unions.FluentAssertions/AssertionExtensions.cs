using FluentAssertions;
using FluentAssertions.Execution;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.FluentAssertions;

/// <summary>
/// Entry points that start assertions on a <see cref="Result"/>, <see cref="Result{T}"/>, <see cref="Option{T}"/> or a custom result type.
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

    /// <summary>
    /// Starts assertions on a custom result type that implements <see cref="IResult{TSelf, T, TError}"/>.
    /// </summary>
    [CustomAssertion]
    public static CustomResultAssertions<TSelf, T, TError> Should<TSelf, T, TError>(this IResult<TSelf, T, TError> subject)
        where TSelf : struct, IResult<TSelf, T, TError>
        where TError : notnull
    {
        return new CustomResultAssertions<TSelf, T, TError>((TSelf)subject, AssertionChain.GetOrCreate());
    }
}
