using System.Linq.Expressions;
using AwesomeAssertions;
using AwesomeAssertions.Execution;

namespace Fluent.Unions.AwesomeAssertions;

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

    /// <summary>
    /// Asserts that the result is Ok and exposes its value through <c>Which</c>.
    /// </summary>
    [CustomAssertion]
    public AndWhichConstraint<ResultAssertions<T>, T> BeOk(string because = "", params object[] becauseArgs)
    {
        ResultChecks.ExpectOk(assertionChain, Subject.Value, Subject.Success, because, becauseArgs);

        return new AndWhichConstraint<ResultAssertions<T>, T>(this, Subject.Success ? (T)Subject.Value! : default!, assertionChain, ".Value");
    }

    /// <summary>
    /// Asserts that the result is Ok and holds <paramref name="expected"/>.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<ResultAssertions<T>> BeOkWithValue(T expected, string because = "", params object[] becauseArgs)
    {
        ResultChecks.ExpectOk(assertionChain, Subject.Value, Subject.Success, because, becauseArgs);

        if (Subject.Success)
        {
            var actual = (T)Subject.Value!;

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

        ResultChecks.ExpectOk(assertionChain, Subject.Value, Subject.Success, because, becauseArgs);

        if (Subject.Success)
        {
            var actual = (T)Subject.Value!;

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

        ResultChecks.ExpectOk(assertionChain, Subject.Value, Subject.Success, because, becauseArgs);

        if (Subject.Success)
        {
            assertion((T)Subject.Value!);
        }

        return new AndConstraint<ResultAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the result is empty: neither a value nor an error.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<ResultAssertions<T>> BeEmpty(string because = "", params object[] becauseArgs)
    {
        assertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Value is null)
            .FailWith("Expected {context:result} to be empty{reason}, but it was {0}.", ResultChecks.Describe(Subject.Value));

        return new AndConstraint<ResultAssertions<T>>(this);
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
