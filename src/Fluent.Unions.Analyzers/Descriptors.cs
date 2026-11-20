using Microsoft.CodeAnalysis;

namespace Fluent.Unions.Analyzers;

/// <summary>
/// The diagnostics of all Fluent.Unions analyzers.
/// </summary>
public static class Descriptors
{
    /// <summary>
    /// The diagnostic property that names the operator a code fix renames the call to.
    /// </summary>
    public const string ReplacementProperty = "Replacement";

    private const string HelpLink = "https://tobistr.github.io/Fluent.Unions/analyzer/#";

    /// <summary>FLU0001: a result is dropped.</summary>
    public static readonly DiagnosticDescriptor UnusedResult = Create(
        "FLU0001",
        "Result is not used",
        "The result of '{0}' is not used",
        "An ignored result hides its error. Handle it, or discard it explicitly with '_ = ...'.",
        DiagnosticSeverity.Warning);

    /// <summary>FLU0002: an async lambda is passed to an operator that does not await it.</summary>
    public static readonly DiagnosticDescriptor AsyncLambdaInSyncOperator = Create(
        "FLU0002",
        "Async lambda passed to a synchronous operator",
        "'{0}' does not await the async lambda, so its exception escapes the result; use '{1}'",
        "A synchronous operator wraps the task the lambda returns instead of its outcome, which gives a result of a task. An exception inside the task is never caught.",
        DiagnosticSeverity.Warning);

    /// <summary>FLU0003: an async lambda becomes async void.</summary>
    public static readonly DiagnosticDescriptor AsyncVoidLambda = Create(
        "FLU0003",
        "Async lambda becomes async void",
        "The async lambda passed to '{0}' is async void; {1}",
        "An async lambda passed as an Action is async void. Nobody awaits it, and an exception from it can crash the process.",
        DiagnosticSeverity.Warning);

    /// <summary>FLU0004: a type argument overlaps another case of the union.</summary>
    public static readonly DiagnosticDescriptor OverlappingTypeArgument = Create(
        "FLU0004",
        "Type argument overlaps another case",
        "'{0}' cannot tell its cases apart, because the type argument '{1}' overlaps the case '{2}'",
        "A union decides its case by the runtime type of the value. When a type argument overlaps another case, a value of that type is read as the other case.",
        DiagnosticSeverity.Error);

    /// <summary>FLU0005: a type argument is nullable.</summary>
    public static readonly DiagnosticDescriptor NullableTypeArgument = Create(
        "FLU0005",
        "Nullable type argument",
        "'{0}' cannot hold null; use a non-nullable type argument, or Option<T> for a missing value",
        "A null value does not become a case of the union: a result holds an error instead, and an option holds None.",
        DiagnosticSeverity.Error);

    /// <summary>FLU0006: Select returns a nested result or option.</summary>
    public static readonly DiagnosticDescriptor NestedSelect = Create(
        "FLU0006",
        "Select returns a nested result",
        "'{0}' gives a nested '{1}'; use '{2}'",
        "A selector that returns a result or an option nests it inside another one. Chain flattens it.",
        DiagnosticSeverity.Error);

    /// <summary>FLU0007: a result is created with default.</summary>
    public static readonly DiagnosticDescriptor DefaultResult = Create(
        "FLU0007",
        "Result created with default",
        "A default '{0}' is an error; create it with a value or an error",
        "A default result holds neither a value nor an error, so it counts as an error that says it was not initialized.",
        DiagnosticSeverity.Warning);

    /// <summary>FLU0008: a result is serialized to JSON.</summary>
    public static readonly DiagnosticDescriptor SerializedResult = Create(
        "FLU0008",
        "Result is serialized",
        "'{0}' throws NotSupportedException when it is serialized; {1}",
        "Results refuse to serialize, because an error would expose exception details. Convert the result first.",
        DiagnosticSeverity.Warning);

    /// <summary>FLU0009: an error factory throws or returns null.</summary>
    public static readonly DiagnosticDescriptor ThrowingErrorFactory = Create(
        "FLU0009",
        "Error factory throws or returns null",
        "'{0}' must not {1}",
        "FromException and FromError turn failures into results. If they throw or return null, the failure escapes the result.",
        DiagnosticSeverity.Warning);

    /// <summary>FLU0010: GetValue or GetError is called without a check.</summary>
    public static readonly DiagnosticDescriptor UncheckedAccess = Create(
        "FLU0010",
        "Result is read without a check",
        "'{0}' throws when the result {1}; check it first, or use TryGetValue, Match or switch",
        "GetValue and GetError throw on the other case. Read the result after a check, or with an operation that handles both cases.",
        DiagnosticSeverity.Info);

    /// <summary>FLU0011: the Value of a union is cast.</summary>
    public static readonly DiagnosticDescriptor ValueCast = Create(
        "FLU0011",
        "Value is cast",
        "The cast of '{0}' throws when it holds another case; use a pattern or TryGetValue",
        "Value holds whichever case the union has. A cast to one case throws InvalidCastException on every other case.",
        DiagnosticSeverity.Info);

    /// <summary>FLU0012: a method that returns a result throws.</summary>
    public static readonly DiagnosticDescriptor ThrowInResultMethod = Create(
        "FLU0012",
        "Throw in a method that returns a result",
        "Return the exception instead of throwing it; '{0}' returns a result",
        "A method that returns a result promises to report failures in it. A thrown exception bypasses that promise.",
        DiagnosticSeverity.Info);

    /// <summary>FLU0013: ToOption drops the error of a result.</summary>
    public static readonly DiagnosticDescriptor ResultToOption = Create(
        "FLU0013",
        "ToOption drops the error",
        "ToOption drops the error of the result; handle or log it first",
        "ToOption turns an error into None without a trace. Log it with OnErrorDo first, or keep the result.",
        DiagnosticSeverity.Info);

    private static DiagnosticDescriptor Create(string id, string title, string message, string description, DiagnosticSeverity severity) =>
        new(id, title, message, "Usage", severity, isEnabledByDefault: true, description, HelpLink + id.ToLowerInvariant());
}
