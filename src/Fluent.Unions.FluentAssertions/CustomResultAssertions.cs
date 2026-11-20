using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using FluentAssertions;
using FluentAssertions.Execution;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.FluentAssertions;

/// <summary>
/// Assertions on the state and value of a custom result type that implements <see cref="IResult{TSelf, T, TError}"/>.
/// </summary>
public class CustomResultAssertions<TSelf, T, TError>
    where TSelf : struct, IResult<TSelf, T, TError>
    where TError : notnull
{
    private readonly AssertionChain assertionChain;

    internal CustomResultAssertions(TSelf subject, AssertionChain assertionChain)
    {
        Subject = subject;
        this.assertionChain = assertionChain;
    }

    /// <summary>
    /// The result under test.
    /// </summary>
    public TSelf Subject { get; }

    private object? Case => Subject.TryGetError(out var error) ? error : Subject.Value;

    /// <summary>
    /// Asserts that the result is Ok and exposes its value through <c>Which</c>.
    /// </summary>
    [CustomAssertion]
    public AndWhichConstraint<CustomResultAssertions<TSelf, T, TError>, T> BeOk(string because = "", params object[] becauseArgs)
    {
        var success = TryGetValue(out var value);

        ExpectOk(success, because, becauseArgs);

        return new AndWhichConstraint<CustomResultAssertions<TSelf, T, TError>, T>(this, value, assertionChain, ".Value");
    }

    /// <summary>
    /// Asserts that the result is Ok and holds <paramref name="expected"/>.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<CustomResultAssertions<TSelf, T, TError>> BeOkWithValue(T expected, string because = "", params object[] becauseArgs)
    {
        var success = TryGetValue(out var actual);

        ExpectOk(success, because, becauseArgs);

        if (success)
        {
            assertionChain
                .ForCondition(EqualityComparer<T>.Default.Equals(actual, expected))
                .FailWith("Expected {context:result} to be Ok with value {0}{reason}, but it was Ok with value {1}.", expected, actual);
        }

        return new AndConstraint<CustomResultAssertions<TSelf, T, TError>>(this);
    }

    /// <summary>
    /// Asserts that the result is Ok and its value matches <paramref name="predicate"/>.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<CustomResultAssertions<TSelf, T, TError>> BeOkMatching(Expression<Func<T, bool>> predicate, string because = "", params object[] becauseArgs)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var success = TryGetValue(out var actual);

        ExpectOk(success, because, becauseArgs);

        if (success)
        {
            assertionChain
                .ForCondition(predicate.Compile()(actual))
                .FailWith("Expected {context:result} to be Ok with a value matching {0}{reason}, but it was Ok with value {1}.", predicate.Body, actual);
        }

        return new AndConstraint<CustomResultAssertions<TSelf, T, TError>>(this);
    }

    /// <summary>
    /// Asserts that the result is an error and exposes it through <c>Which</c>.
    /// </summary>
    [CustomAssertion]
    public AndWhichConstraint<CustomResultAssertions<TSelf, T, TError>, TError> BeError(string because = "", params object[] becauseArgs)
    {
        return ExpectError<TError>(null, because, becauseArgs);
    }

    /// <summary>
    /// Asserts that the error is a <typeparamref name="TCase"/>, also when it is a case of an error union, and exposes it through <c>Which</c>.
    /// </summary>
    [CustomAssertion]
    public AndWhichConstraint<CustomResultAssertions<TSelf, T, TError>, TCase> BeError<TCase>(string because = "", params object[] becauseArgs)
    {
        return ExpectError<TCase>(null, because, becauseArgs);
    }

    /// <summary>
    /// Asserts that the error is a <typeparamref name="TCase"/> that matches <paramref name="predicate"/>.
    /// </summary>
    [CustomAssertion]
    public AndWhichConstraint<CustomResultAssertions<TSelf, T, TError>, TCase> BeError<TCase>(Expression<Func<TCase, bool>> predicate, string because = "", params object[] becauseArgs)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return ExpectError(predicate, because, becauseArgs);
    }

    private AndWhichConstraint<CustomResultAssertions<TSelf, T, TError>, TCase> ExpectError<TCase>(
        Expression<Func<TCase, bool>>? predicate,
        string because,
        object[] becauseArgs)
    {
        var found = TryGetCase<TCase>(out var match);

        assertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(found)
            .FailWith("Expected {context:result} to be an error of type {0}{reason}, but it was {1}.", typeof(TCase), Describe());

        if (found && predicate is not null)
        {
            assertionChain
                .ForCondition(predicate.Compile()(match))
                .FailWith("Expected {context:result} to be an error matching {0}{reason}, but it was {1}.", predicate.Body, Describe());
        }

        return new AndWhichConstraint<CustomResultAssertions<TSelf, T, TError>, TCase>(this, match, assertionChain, ".Error");
    }

    private void ExpectOk(bool success, string because, object[] becauseArgs)
    {
        assertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(success)
            .FailWith("Expected {context:result} to be Ok{reason}, but it was {0}.", Describe());
    }

    private bool TryGetValue(out T value)
    {
        if (Case is not TError && Case is T payload)
        {
            value = payload;
            return true;
        }

        value = default!;
        return false;
    }

    private bool TryGetCase<TCase>(out TCase match)
    {
        if (Case is TError error)
        {
            switch (error)
            {
                case TCase direct:
                    match = direct;
                    return true;
                case IUnion { Value: TCase inner }:
                    match = inner;
                    return true;
            }
        }

        match = default!;
        return false;
    }

    private Described Describe()
    {
        if (Case is TError error and not Exception)
        {
            var shown = error is IUnion { Value: { } inner } ? inner : error;

            return new Described($"an error of type {shown.GetType().FullName} ({shown})");
        }

        return ResultChecks.Describe(Case);
    }
}
