using System.Linq.Expressions;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Fluent.Unions.FluentAssertions;

/// <summary>
/// Assertions on the state and value of a <see cref="Result{T}"/>.
/// </summary>
public class ResultAssertions<T>
{
    private readonly AssertionChain assertionChain;

    internal ResultAssertions(Result<T> subject, AssertionChain assertionChain)
    {
        Subject = subject;
        this.assertionChain = assertionChain;
    }

    /// <summary>
    /// The result under test.
    /// </summary>
    public Result<T> Subject { get; }

    private object? Case => Subject.TryGetError(out var error) ? error : Subject.Value;

    /// <summary>
    /// Asserts that the result is Ok and exposes its value through <c>Which</c>.
    /// </summary>
    [CustomAssertion]
    public AndWhichConstraint<ResultAssertions<T>, T> BeOk(string because = "", params object[] becauseArgs)
    {
        ResultChecks.ExpectOk(assertionChain, Case, Subject.IsSuccess, because, becauseArgs);

        return new AndWhichConstraint<ResultAssertions<T>, T>(this, Subject.IsSuccess ? (T)Case! : default!, assertionChain, ".Value");
    }

    /// <summary>
    /// Asserts that the result is Ok and holds <paramref name="expected"/>.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<ResultAssertions<T>> BeOkWithValue(T expected, string because = "", params object[] becauseArgs)
    {
        ResultChecks.ExpectOk(assertionChain, Case, Subject.IsSuccess, because, becauseArgs);

        if (Subject.IsSuccess)
        {
            var actual = (T)Case!;

            assertionChain
                .ForCondition(EqualityComparer<T>.Default.Equals(actual, expected))
                .FailWith("Expected {context:result} to be Ok with value {0}{reason}, but it was Ok with value {1}.", expected, actual);
        }

        return new AndConstraint<ResultAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the result is Ok and its value matches <paramref name="predicate"/>.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<ResultAssertions<T>> BeOkMatching(Expression<Func<T, bool>> predicate, string because = "", params object[] becauseArgs)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        ResultChecks.ExpectOk(assertionChain, Case, Subject.IsSuccess, because, becauseArgs);

        if (Subject.IsSuccess)
        {
            var actual = (T)Case!;

            assertionChain
                .ForCondition(predicate.Compile()(actual))
                .FailWith("Expected {context:result} to be Ok with a value matching {0}{reason}, but it was Ok with value {1}.", predicate.Body, actual);
        }

        return new AndConstraint<ResultAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the result is Ok and runs <paramref name="assertion"/> on its value.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<ResultAssertions<T>> BeOkSatisfying(Action<T> assertion, string because = "", params object[] becauseArgs)
    {
        ArgumentNullException.ThrowIfNull(assertion);

        ResultChecks.ExpectOk(assertionChain, Case, Subject.IsSuccess, because, becauseArgs);

        if (Subject.IsSuccess)
        {
            assertion((T)Case!);
        }

        return new AndConstraint<ResultAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the result is an error.
    /// </summary>
    [CustomAssertion]
    public ErrorAssertions<Exception> BeError(string because = "", params object[] becauseArgs)
    {
        return ResultChecks.ExpectError<Exception>(assertionChain, Case, null, because, becauseArgs);
    }

    /// <summary>
    /// Asserts that the result is an error of type <typeparamref name="TException"/> or a derived type.
    /// </summary>
    [CustomAssertion]
    public ErrorAssertions<TException> BeError<TException>(string because = "", params object[] becauseArgs)
        where TException : Exception
    {
        return ResultChecks.ExpectError<TException>(assertionChain, Case, null, because, becauseArgs);
    }

    /// <summary>
    /// Asserts that the result is an error of type <typeparamref name="TException"/> that matches <paramref name="predicate"/>.
    /// </summary>
    [CustomAssertion]
    public ErrorAssertions<TException> BeError<TException>(Expression<Func<TException, bool>> predicate, string because = "", params object[] becauseArgs)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return ResultChecks.ExpectError(assertionChain, Case, predicate, because, becauseArgs);
    }

    /// <summary>
    /// Asserts that the result is an error that holds an <see cref="AggregateException"/>.
    /// </summary>
    [CustomAssertion]
    public AggregateErrorAssertions BeAggregateError(string because = "", params object[] becauseArgs)
    {
        return ResultChecks.ExpectAggregateError(assertionChain, Case, because, becauseArgs);
    }
}
