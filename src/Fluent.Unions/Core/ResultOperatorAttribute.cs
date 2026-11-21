using System.ComponentModel;

namespace Fluent.Unions.Core;

/// <summary>
/// Marks a generated operator, so the analyzers treat it like an operator of the library.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ResultOperatorAttribute : Attribute;
