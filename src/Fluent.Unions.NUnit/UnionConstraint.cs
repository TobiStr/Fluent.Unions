using NUnit.Framework.Constraints;

namespace Fluent.Unions.NUnit;

/// <summary>
/// A constraint on the state of a <see cref="Result"/>, <see cref="Result{T}"/> or <see cref="Option{T}"/>.
/// </summary>
public sealed class UnionConstraint : Constraint
{
    private readonly Func<UnionState, bool> isSatisfied;

    internal UnionConstraint(string description, Func<UnionState, bool> isSatisfied)
    {
        Description = description;
        this.isSatisfied = isSatisfied;
    }

    /// <inheritdoc/>
    public override string Description { get; }

    /// <inheritdoc/>
    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        var state = UnionState.Of(actual);

        return new UnionConstraintResult(this, state, isSatisfied(state));
    }

    private sealed class UnionConstraintResult(IConstraint constraint, UnionState state, bool isSuccess)
        : ConstraintResult(constraint, state.Payload, isSuccess)
    {
        public override void WriteActualValueTo(MessageWriter writer) => state.WriteTo(writer);
    }
}
