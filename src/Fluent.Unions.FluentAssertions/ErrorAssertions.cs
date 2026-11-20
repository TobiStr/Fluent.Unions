using FluentAssertions;
using FluentAssertions.Execution;

namespace Fluent.Unions.FluentAssertions;

/// <summary>
/// Assertions on the exception inside an error result.
/// </summary>
public class ErrorAssertions<TException>
    where TException : Exception
{
    private readonly AssertionChain assertionChain;

    internal ErrorAssertions(TException? subject, AssertionChain assertionChain)
    {
        Subject = subject;
        this.assertionChain = assertionChain;
    }

    /// <summary>
    /// The exception under test, or null when an earlier assertion in a scope failed.
    /// </summary>
    public TException? Subject { get; }

    /// <summary>
    /// The exception under test.
    /// </summary>
    public TException Which => Subject!;

    /// <summary>
    /// Continues with more assertions on the same exception.
    /// </summary>
    public ErrorAssertions<TException> And => this;

    /// <summary>
    /// Asserts that the message matches <paramref name="expectedWildcardPattern"/>, where <c>*</c> and <c>?</c> are wildcards and case is ignored.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<ErrorAssertions<TException>> WithMessage(string expectedWildcardPattern, string because = "", params object[] becauseArgs)
    {
        ArgumentNullException.ThrowIfNull(expectedWildcardPattern);

        if (Subject is not null)
        {
            assertionChain
                .BecauseOf(because, becauseArgs)
                .ForCondition(ResultChecks.IsWildcardMatch(expectedWildcardPattern, Subject.Message))
                .FailWith("Expected the message of {context:error} to match {0}{reason}, but it was {1}.", expectedWildcardPattern, Subject.Message);
        }

        return new AndConstraint<ErrorAssertions<TException>>(this);
    }

    /// <summary>
    /// Asserts that the message contains <paramref name="expected"/>, ignoring case.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<ErrorAssertions<TException>> WithMessageContaining(string expected, string because = "", params object[] becauseArgs)
    {
        ArgumentNullException.ThrowIfNull(expected);

        if (Subject is not null)
        {
            assertionChain
                .BecauseOf(because, becauseArgs)
                .ForCondition(Subject.Message.Contains(expected, StringComparison.OrdinalIgnoreCase))
                .FailWith("Expected the message of {context:error} to contain {0}{reason}, but it was {1}.", expected, Subject.Message);
        }

        return new AndConstraint<ErrorAssertions<TException>>(this);
    }
}
