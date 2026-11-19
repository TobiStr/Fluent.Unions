namespace Fluent.Unions.Validation;

public static partial class Is
{
    /// <summary>True if the character is a decimal digit.</summary>
    public static bool Digit(char value) => char.IsDigit(value);

    /// <summary>True if the character is a letter.</summary>
    public static bool Letter(char value) => char.IsLetter(value);

    /// <summary>True if the character is a letter or a decimal digit.</summary>
    public static bool LetterOrDigit(char value) => char.IsLetterOrDigit(value);

    /// <summary>True if the character is a lowercase letter.</summary>
    public static bool Lower(char value) => char.IsLower(value);

    /// <summary>True if the character is an uppercase letter.</summary>
    public static bool Upper(char value) => char.IsUpper(value);

    /// <summary>True if the character is punctuation.</summary>
    public static bool Punctuation(char value) => char.IsPunctuation(value);

    /// <summary>True if the character is not a letter, digit or white space.</summary>
    public static bool Special(char value) => !char.IsLetterOrDigit(value) && !char.IsWhiteSpace(value);

    /// <summary>True if the character is a symbol.</summary>
    public static bool Symbol(char value) => char.IsSymbol(value);

    /// <summary>True if the character is white space.</summary>
    public static bool WhiteSpace(char value) => char.IsWhiteSpace(value);
}
