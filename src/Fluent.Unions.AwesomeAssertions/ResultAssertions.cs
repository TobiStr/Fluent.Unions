using System.Linq.Expressions;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using AwesomeAssertions.Execution;

namespace Fluent.Unions.AwesomeAssertions;

/// <summary>
/// Assertions on the state of a <see cref="Result"/>.
/// </summary>
public class ResultAssertions
{
    private readonly AssertionChain assertionChain;

    internal ResultAssertions(Result subject, AssertionChain assertionChain)
    {
        Subject = subject;
        this.assertionChain = assertionChain;
    }

    /// <summary>
    /// The result under test.
    /// </summary>
    public Result Subject { get; }

    /// <summary>
    /// Asserts that the result is Ok.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<ResultAssertions> BeOk(string because = "", params object[] becauseArgs)
    {
        ResultChecks.ExpectOk(assertionChain, Subject.Value, Subject.Success, because, becauseArgs);

        return new AndConstraint<ResultAssertions>(this);
    }

    /// <summary>
    /// Asserts that the result is an error.
    /// </summary>
    [CustomAssertion]
    public ErrorAssertions<Exception> BeError(string because = "", params object[] becauseArgs)
    {
        return ResultChecks.ExpectError<Exception>(assertionChain, Subject.Value, null, because, becauseArgs);
    }

    /// <summary>
    /// Asserts that the result is an error of type <typeparamref name="TException"/> or a derived type.
    /// </summary>
    [CustomAssertion]
    public ErrorAssertions<TException> BeError<TException>(string because = "", params object[] becauseArgs)
        where TException : Exception
    {
        return ResultChecks.ExpectError<TException>(assertionChain, Subject.Value, null, because, becauseArgs);
    }

    /// <summary>
    /// Asserts that the result is an error of type <typeparamref name="TException"/> that matches <paramref name="predicate"/>.
    /// </summary>
    [CustomAssertion]
    public ErrorAssertions<TException> BeError<TException>(Expression<Func<TException, bool>> predicate, string because = "", params object[] becauseArgs)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return ResultChecks.ExpectError(assertionChain, Subject.Value, predicate, because, becauseArgs);
    }

    /// <summary>
    /// Asserts that the result is an error that holds an <see cref="AggregateException"/>.
    /// </summary>
    [CustomAssertion]
    public AggregateErrorAssertions BeAggregateError(string because = "", params object[] becauseArgs)
    {
        return ResultChecks.ExpectAggregateError(assertionChain, Subject.Value, because, becauseArgs);
    }
}

internal sealed class Described(string text)
{
    public override string ToString() => text;
}

internal static class ResultChecks
{
    internal static Described Describe(object? value)
    {
        return new Described(value switch
        {
            null => "empty",
            Exception error => $"an error of type {error.GetType().FullName} with message \"{error.Message}\"",
            _ => "Ok",
        });
    }

    internal static bool IsWildcardMatch(string pattern, string text)
    {
        var expression = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";

        return Regex.IsMatch(text, expression, RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(1));
    }

    internal static void ExpectOk(AssertionChain chain, object? value, bool success, string because, object[] becauseArgs)
    {
        chain
            .BecauseOf(because, becauseArgs)
            .ForCondition(success)
            .FailWith("Expected {context:result} to be Ok{reason}, but it was {0}.", Describe(value));
    }

    internal static ErrorAssertions<TException> ExpectError<TException>(
        AssertionChain chain,
        object? value,
        Expression<Func<TException, bool>>? predicate,
        string because,
        object[] becauseArgs)
        where TException : Exception
    {
        var error = value as TException;

        chain
            .BecauseOf(because, becauseArgs)
            .ForCondition(error is not null)
            .FailWith("Expected {context:result} to be an error of type {0}{reason}, but it was {1}.", typeof(TException), Describe(value));

        if (error is not null && predicate is not null)
        {
            chain
                .ForCondition(predicate.Compile()(error))
                .FailWith("Expected {context:result} to be an error matching {0}{reason}, but it was {1}.", predicate.Body, Describe(value));
        }

        return new ErrorAssertions<TException>(error, chain);
    }

    internal static AggregateErrorAssertions ExpectAggregateError(AssertionChain chain, object? value, string because, object[] becauseArgs)
    {
        var error = value as AggregateException;

        chain
            .BecauseOf(because, becauseArgs)
            .ForCondition(error is not null)
            .FailWith("Expected {context:result} to be an aggregate error{reason}, but it was {0}.", Describe(value));

        return new AggregateErrorAssertions(error, chain);
    }
}
