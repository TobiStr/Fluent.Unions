using AwesomeAssertions;
using AwesomeAssertions.Execution;

namespace Fluent.Unions.AwesomeAssertions;

/// <summary>
/// Assertions on the flattened inner exceptions of an aggregate error.
/// </summary>
public class AggregateErrorAssertions
{
    private readonly AssertionChain assertionChain;

    internal AggregateErrorAssertions(AggregateException? subject, AssertionChain assertionChain)
    {
        Subject = subject;
        Inner = subject?.Flatten().InnerExceptions ?? [];
        this.assertionChain = assertionChain;
    }

    /// <summary>
    /// The aggregate exception under test, or null when an earlier assertion in a scope failed.
    /// </summary>
    public AggregateException? Subject { get; }

    /// <summary>
    /// The flattened inner exceptions.
    /// </summary>
    public IReadOnlyList<Exception> Inner { get; }

    /// <summary>
    /// Continues with more assertions on the same aggregate.
    /// </summary>
    public AggregateErrorAssertions And => this;

    /// <summary>
    /// Asserts that there are exactly <paramref name="expected"/> inner exceptions.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<AggregateErrorAssertions> WithInnerCount(int expected, string because = "", params object[] becauseArgs)
    {
        if (Subject is not null)
        {
            assertionChain
                .BecauseOf(because, becauseArgs)
                .ForCondition(Inner.Count == expected)
                .FailWith("Expected {context:error} to have {0} inner exception(s){reason}, but it had {1}: {2}.", expected, Inner.Count, DescribeInner());
        }

        return new AndConstraint<AggregateErrorAssertions>(this);
    }

    /// <summary>
    /// Asserts that an inner exception has type <typeparamref name="TException"/> and exposes the first match through <c>Which</c>.
    /// </summary>
    [CustomAssertion]
    public AndWhichConstraint<AggregateErrorAssertions, TException> WithInner<TException>(string because = "", params object[] becauseArgs)
        where TException : Exception
    {
        var match = Inner.OfType<TException>().FirstOrDefault();

        if (Subject is not null)
        {
            assertionChain
                .BecauseOf(because, becauseArgs)
                .ForCondition(match is not null)
                .FailWith("Expected {context:error} to have an inner exception of type {0}{reason}, but it had {1}.", typeof(TException), DescribeInner());
        }

        return new AndWhichConstraint<AggregateErrorAssertions, TException>(this, match!, assertionChain, ".InnerExceptions");
    }

    /// <summary>
    /// Asserts that an inner exception message matches <paramref name="expectedWildcardPattern"/>, where <c>*</c> and <c>?</c> are wildcards and case is ignored.
    /// </summary>
    [CustomAssertion]
    public AndConstraint<AggregateErrorAssertions> WithInnerMessage(string expectedWildcardPattern, string because = "", params object[] becauseArgs)
    {
        ArgumentNullException.ThrowIfNull(expectedWildcardPattern);

        if (Subject is not null)
        {
            assertionChain
                .BecauseOf(because, becauseArgs)
                .ForCondition(Inner.Any(error => ResultChecks.IsWildcardMatch(expectedWildcardPattern, error.Message)))
                .FailWith("Expected {context:error} to have an inner exception with a message matching {0}{reason}, but it had {1}.", expectedWildcardPattern, DescribeInner());
        }

        return new AndConstraint<AggregateErrorAssertions>(this);
    }

    private Described DescribeInner()
    {
        return new Described(Inner.Count == 0
            ? "none"
            : string.Join("; ", Inner.Select(error => $"{error.GetType().FullName} \"{error.Message}\"")));
    }
}
