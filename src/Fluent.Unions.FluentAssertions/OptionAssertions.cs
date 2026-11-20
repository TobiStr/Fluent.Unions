using System.Linq.Expressions;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Fluent.Unions.FluentAssertions;

/// <summary>
/// Assertions on the state and value of an <see cref="Option{T}"/>.
/// </summary>
public class OptionAssertions<T>
{
    private readonly AssertionChain assertionChain;

    internal OptionAssertions(Option<T> subject, AssertionChain assertionChain)
    {
        Subject = subject;
        this.assertionChain = assertionChain;
    }

    /// <summary>
    /// The option under test.
    /// </summary>
    public Option<T> Subject { get; }

    /// <summary>
    /// Asserts that the option has a value and exposes it through <c>Which</c>.
    /// </summary>
    [CustomAssertion]
    public AndWhichConstraint<OptionAssertions<T>, T> BeSome(string because = "", params object[] becauseArgs)
    {
        ExpectSome(because, becauseArgs);

        return new AndWhichConstraint<OptionAssertions<T>, T>(this, Subject.TryGetValue(out var value) ? value : default!, assertionChain, ".Value");
    }

    /// <summary>
    /// Asserts that the option has a value equal to <paramref name="expected"/>.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<OptionAssertions<T>> BeSomeWithValue(T expected, string because = "", params object[] becauseArgs)
    {
        ExpectSome(because, becauseArgs);

        if (Subject.TryGetValue(out var actual))
        {
            assertionChain
                .ForCondition(EqualityComparer<T>.Default.Equals(actual, expected))
                .FailWith("Expected {context:option} to be some with value {0}{reason}, but it was some with value {1}.", expected, actual);
        }

        return new AndConstraint<OptionAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the option is none or has a value different from <paramref name="unexpected"/>.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<OptionAssertions<T>> NotBeSomeWithValue(T unexpected, string because = "", params object[] becauseArgs)
    {
        var isUnexpected = Subject.TryGetValue(out var actual) && EqualityComparer<T>.Default.Equals(actual, unexpected);

        assertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(!isUnexpected)
            .FailWith("Did not expect {context:option} to be some with value {0}{reason}.", unexpected);

        return new AndConstraint<OptionAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the option has a value that matches <paramref name="predicate"/>.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<OptionAssertions<T>> BeSomeMatching(Expression<Func<T, bool>> predicate, string because = "", params object[] becauseArgs)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        ExpectSome(because, becauseArgs);

        if (Subject.TryGetValue(out var actual))
        {
            assertionChain
                .ForCondition(predicate.Compile()(actual))
                .FailWith("Expected {context:option} to be some with a value matching {0}{reason}, but it was some with value {1}.", predicate.Body, actual);
        }

        return new AndConstraint<OptionAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the option has a value and runs <paramref name="assertion"/> on it.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<OptionAssertions<T>> BeSomeSatisfying(Action<T> assertion, string because = "", params object[] becauseArgs)
    {
        ArgumentNullException.ThrowIfNull(assertion);

        ExpectSome(because, becauseArgs);

        if (Subject.TryGetValue(out var actual))
        {
            assertion(actual);
        }

        return new AndConstraint<OptionAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the option has no value. A default option counts as none.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<OptionAssertions<T>> BeNone(string because = "", params object[] becauseArgs)
    {
        assertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(!Subject.HasValue)
            .FailWith("Expected {context:option} to be none{reason}, but it was some with value {0}.", Subject.TryGetValue(out var value) ? value : default);

        return new AndConstraint<OptionAssertions<T>>(this);
    }

    private void ExpectSome(string because, object[] becauseArgs)
    {
        assertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.HasValue)
            .FailWith("Expected {context:option} to be some{reason}, but it was none.");
    }
}
