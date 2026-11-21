using Fluent.Unions;
using Fluent.Unions.MessagePack;
using MessagePack;
using NUnit.Framework;

namespace MessagePackDomain.Tests;

public class UserDeclaredResultTests
{
    private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard.WithUnions();
    private static readonly byte[] Nil = [MessagePackCode.Nil];

    [Test]
    public void UserDeclaredResultRefusesToSerialize()
    {
        Result<int> error = new DomainException("Secret");

        var exception = Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Serialize(error, Options))!;

        Assert.That(exception.InnerException, Is.TypeOf<NotSupportedException>());
        Assert.That(exception.ToString(), Does.Not.Contain("Secret"));
        Assert.That(() => MessagePackSerializer.Serialize(Result.Ok, Options),
            Throws.InstanceOf<MessagePackSerializationException>().With.InnerException.TypeOf<NotSupportedException>());
    }

    [Test]
    public void UserDeclaredResultRefusesToDeserialize()
    {
        Assert.That(() => MessagePackSerializer.Deserialize<Result>(Nil, Options),
            Throws.InstanceOf<MessagePackSerializationException>().With.InnerException.TypeOf<NotSupportedException>());
        Assert.That(() => MessagePackSerializer.Deserialize<Result<int>>(Nil, Options),
            Throws.InstanceOf<MessagePackSerializationException>().With.InnerException.TypeOf<NotSupportedException>());
    }

    [Test]
    public void EveryUserDeclaredResultHasARefusingFormatter()
    {
        Assert.That(UnionsResolver.Instance.GetFormatter<Result>(), Is.Not.Null);
        Assert.That(UnionsResolver.Instance.GetFormatter<Result<string>>(), Is.Not.Null);
    }
}
