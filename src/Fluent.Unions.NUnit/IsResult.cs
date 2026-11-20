using NUnit.Framework.Constraints;

namespace Fluent.Unions.NUnit;

/// <summary>
/// NUnit constraints for <see cref="Result"/> and <see cref="Result{T}"/>, for use with <c>Assert.That</c>.
/// </summary>
public static class IsResult
{
    /// <summary>
    /// Matches an Ok result.
    /// </summary>
    public static UnionConstraint Ok => new("an ok result", state => state.Kind == UnionKind.Ok);

    /// <summary>
    /// Matches an error result, including a default one.
    /// </summary>
    public static UnionConstraint Error => new("an error result", state => state.Kind == UnionKind.Error);

    /// <summary>
    /// Matches an Ok result whose value satisfies <paramref name="expression"/>.
    /// </summary>
    public static UnionConstraint OkWith(IResolveConstraint expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var constraint = expression.Resolve();

        return new($"an ok result with value {constraint.Description}", state => state.Kind == UnionKind.Ok && constraint.ApplyTo(state.Payload).IsSuccess);
    }

    /// <summary>
    /// Matches an error result whose exception satisfies <paramref name="expression"/>.
    /// </summary>
    public static UnionConstraint ErrorWith(IResolveConstraint expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var constraint = expression.Resolve();

        return new($"an error result with exception {constraint.Description}", state => state.Kind == UnionKind.Error && constraint.ApplyTo(state.Payload).IsSuccess);
    }

    /// <summary>
    /// Matches an error result whose exception is a <typeparamref name="TException"/> or a derived type.
    /// </summary>
    public static UnionConstraint ErrorOfType<TException>()
        where TException : Exception
    {
        return new($"an error result of type {typeof(TException).Name}", state => state is { Kind: UnionKind.Error, Payload: TException });
    }
}
