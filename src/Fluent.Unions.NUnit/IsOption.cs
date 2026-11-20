using NUnit.Framework.Constraints;

namespace Fluent.Unions.NUnit;

/// <summary>
/// NUnit constraints for <see cref="Option{T}"/>, for use with <c>Assert.That</c>.
/// </summary>
public static class IsOption
{
    /// <summary>
    /// Matches an option that has a value.
    /// </summary>
    public static UnionConstraint Some => new("an option with a value", state => state.Kind == UnionKind.Some);

    /// <summary>
    /// Matches an option without a value. A default option counts as none.
    /// </summary>
    public static UnionConstraint None => new("an option without a value", state => state.Kind == UnionKind.None);

    /// <summary>
    /// Matches an option whose value satisfies <paramref name="expression"/>.
    /// </summary>
    public static UnionConstraint SomeWith(IResolveConstraint expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var constraint = expression.Resolve();

        return new($"an option with value {constraint.Description}", state => state.Kind == UnionKind.Some && constraint.ApplyTo(state.Payload).IsSuccess);
    }
}
