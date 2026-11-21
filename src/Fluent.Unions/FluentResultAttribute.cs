namespace Fluent.Unions;

/// <summary>
/// Turns a <c>readonly partial struct</c> into a result type with the error type <typeparamref name="TError"/>:
/// <c>[FluentResult&lt;DomainException&gt;] public readonly partial struct Result&lt;T&gt;;</c>.
/// The generator adds the union cases, the companion without a value and every operator.
/// Declare the companion yourself with <c>[FluentResult&lt;DomainException&gt;] public readonly partial struct Result;</c> to add members to it.
/// </summary>
/// <typeparam name="TError">The type of the error.</typeparam>
[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public sealed class FluentResultAttribute<TError> : Attribute
    where TError : notnull
{
    /// <summary>
    /// Converts every case of a union error type straight to the result. Defaults to <c>true</c>.
    /// </summary>
    public bool CaseConversions { get; init; } = true;
}
