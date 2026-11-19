namespace Fluent.Unions.Validation;

public static partial class Is
{
    /// <summary>True if the Guid is not <see cref="Guid.Empty"/>.</summary>
    public static bool NotEmpty(Guid value) => value != Guid.Empty;

    /// <summary>True if the Guid is <see cref="Guid.Empty"/>.</summary>
    public static bool Empty(Guid value) => value == Guid.Empty;

    /// <summary>True if the value is true.</summary>
    public static bool True(bool value) => value;

    /// <summary>True if the value is false.</summary>
    public static bool False(bool value) => !value;

    /// <summary>True if the value is a named member of the enumeration. Combined flag values are rejected.</summary>
    public static bool Defined<TEnum>(TEnum value) where TEnum : struct, Enum => Enum.IsDefined(value);
}
