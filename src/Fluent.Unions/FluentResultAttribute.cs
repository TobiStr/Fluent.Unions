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

    /// <summary>
    /// Which exceptions thrown inside an operator become an error. Defaults to <see cref="ExceptionCapture.All"/>.
    /// </summary>
    public ExceptionCapture Capture { get; init; } = ExceptionCapture.All;

    /// <summary>
    /// Exception types that operators never capture, so they propagate with their original stack trace.
    /// Derived types propagate too.
    /// </summary>
    public Type[] Propagate { get; init; } = [];
}

/// <summary>
/// Which exceptions thrown inside an operator become an error. <see cref="OperationCanceledException"/> always propagates.
/// </summary>
public enum ExceptionCapture
{
    /// <summary>
    /// Every exception becomes an error, through <c>FromException</c> when it is not already one.
    /// </summary>
    All,

    /// <summary>
    /// Only exceptions that the error type declares become an error: the error type itself, or a case of an error union.
    /// Every other exception propagates, so a bug cannot pass as an expected error.
    /// </summary>
    Declared
}
