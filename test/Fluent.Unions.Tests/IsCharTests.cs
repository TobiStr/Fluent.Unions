using Fluent.Unions.Validation;
using Is = Fluent.Unions.Validation.Is;

namespace Fluent.Unions.Tests;

public class IsCharTests
{
    [TestCase('5', true)]
    [TestCase('a', false)]
    [TestCase(' ', false)]
    public void Digit(char value, bool expected) => Assert.That(Is.Digit(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase('a', true)]
    [TestCase('Z', true)]
    [TestCase('5', false)]
    [TestCase('-', false)]
    public void Letter(char value, bool expected) => Assert.That(Is.Letter(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase('a', true)]
    [TestCase('5', true)]
    [TestCase('-', false)]
    [TestCase(' ', false)]
    public void LetterOrDigit(char value, bool expected) => Assert.That(Is.LetterOrDigit(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase('a', true)]
    [TestCase('A', false)]
    [TestCase('5', false)]
    public void Lower(char value, bool expected) => Assert.That(Is.Lower(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase('A', true)]
    [TestCase('a', false)]
    [TestCase('5', false)]
    public void Upper(char value, bool expected) => Assert.That(Is.Upper(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase('!', true)]
    [TestCase('-', true)]
    [TestCase('+', false)]
    [TestCase('$', false)]
    [TestCase('a', false)]
    public void Punctuation(char value, bool expected) => Assert.That(Is.Punctuation(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase('!', true)]
    [TestCase('$', true)]
    [TestCase('+', true)]
    [TestCase(' ', false)]
    [TestCase('a', false)]
    [TestCase('5', false)]
    public void Special(char value, bool expected) => Assert.That(Is.Special(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase('+', true)]
    [TestCase('$', true)]
    [TestCase('!', false)]
    [TestCase('a', false)]
    public void Symbol(char value, bool expected) => Assert.That(Is.Symbol(value), NUnit.Framework.Is.EqualTo(expected));

    [TestCase(' ', true)]
    [TestCase('\t', true)]
    [TestCase('a', false)]
    public void WhiteSpace(char value, bool expected) => Assert.That(Is.WhiteSpace(value), NUnit.Framework.Is.EqualTo(expected));

    [Test]
    public void CharWhiteSpaceDoesNotChangeTheStringRule()
    {
        Func<string?, bool> forStrings = Is.WhiteSpace;
        Func<char, bool> forChars = Is.WhiteSpace;

        Assert.That(forStrings("   "), NUnit.Framework.Is.True);
        Assert.That(forChars(' '), NUnit.Framework.Is.True);
    }
}
