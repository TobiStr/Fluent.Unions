using Grpc.Core;
using NUnit.Framework;

namespace Fluent.Unions.Grpc.Tests;

[TestFixture]
public class ErrorExtensionsTests
{
    [Test]
    public void ValidationError_GivesInvalidArgumentWithItsMessage()
    {
        var exception = new ValidationError("Too short.", "name").ToRpcException();

        Assert.That(exception.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        Assert.That(exception.Status.Detail, Is.EqualTo("Too short."));
    }

    [Test]
    public void ValidationErrors_GiveInvalidArgumentWithAllMessages()
    {
        var errors = new ValidationErrors([new ValidationError("A."), new ValidationError("B.")]);

        var exception = errors.ToRpcException();

        Assert.That(exception.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        Assert.That(exception.Status.Detail, Is.EqualTo("A. B."));
    }

    [TestCase(typeof(KeyNotFoundException), StatusCode.NotFound)]
    [TestCase(typeof(UnauthorizedAccessException), StatusCode.PermissionDenied)]
    [TestCase(typeof(TimeoutException), StatusCode.DeadlineExceeded)]
    public void DefaultMappings_HideTheMessage(Type type, StatusCode expected)
    {
        var error = (Exception)Activator.CreateInstance(type, "Secret")!;

        var exception = error.ToRpcException();

        Assert.That(exception.StatusCode, Is.EqualTo(expected));
        Assert.That(exception.Status.Detail, Is.Empty);
    }

    [Test]
    public void UnmappedError_GivesInternalWithoutDetails()
    {
        var exception = new InvalidOperationException("Secret").ToRpcException();

        Assert.That(exception.StatusCode, Is.EqualTo(StatusCode.Internal));
        Assert.That(exception.Status.Detail, Is.Empty);
    }

    [Test]
    public void RpcException_IsReturnedAsItIs()
    {
        var original = new RpcException(new Status(StatusCode.Aborted, "Retry"));

        Assert.That(original.ToRpcException(), Is.SameAs(original));
    }

    [Test]
    public void Map_ExposesTheMessageOnRequest()
    {
        var options = new ResultGrpcOptions().Map<InvalidOperationException>(StatusCode.FailedPrecondition, exposeMessage: true);

        var exception = new InvalidOperationException("Not now.").ToRpcException(options);

        Assert.That(exception.StatusCode, Is.EqualTo(StatusCode.FailedPrecondition));
        Assert.That(exception.Status.Detail, Is.EqualTo("Not now."));
    }

    [Test]
    public void Map_AppliesToSubclassesAndTheMostSpecificMappingWins()
    {
        var options = new ResultGrpcOptions()
            .Map<ArgumentException>(StatusCode.OutOfRange)
            .Map<ArgumentNullException>(StatusCode.FailedPrecondition);

        Assert.That(new ArgumentOutOfRangeException().ToRpcException(options).StatusCode, Is.EqualTo(StatusCode.OutOfRange));
        Assert.That(new ArgumentNullException().ToRpcException(options).StatusCode, Is.EqualTo(StatusCode.FailedPrecondition));
    }

    [Test]
    public void Map_CanOverrideADefault()
    {
        var options = new ResultGrpcOptions().Map<KeyNotFoundException>(StatusCode.OutOfRange, exposeMessage: true);

        var exception = new KeyNotFoundException("Gone.").ToRpcException(options);

        Assert.That(exception.StatusCode, Is.EqualTo(StatusCode.OutOfRange));
        Assert.That(exception.Status.Detail, Is.EqualTo("Gone."));
    }

    [Test]
    public void ToRpcException_ThrowsForNullError() =>
        Assert.Throws<ArgumentNullException>(() => ErrorExtensions.ToRpcException(null!));
}
