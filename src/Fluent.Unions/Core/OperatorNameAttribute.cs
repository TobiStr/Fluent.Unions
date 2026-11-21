using System.ComponentModel;

namespace Fluent.Unions.Core;

/// <summary>
/// The name the generated operator gets when the core method needs another name to avoid a signature clash.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Method)]
public sealed class OperatorNameAttribute(string name) : Attribute
{
    /// <summary>
    /// The name of the generated operator.
    /// </summary>
    public string Name { get; } = name;
}
