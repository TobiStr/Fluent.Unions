using Fluent.Unions.Grpc.Tests.Protos;
using Grpc.Core;
using NUnit.Framework;

namespace Fluent.Unions.Grpc.Tests;

[TestFixture]
public class CallExtensionsTests
{
    private LookupServer server = null!;

    [OneTimeSetUp]
    public async Task SetUp() =>
        server = await LookupServer.StartAsync(options => options.Map<LookupConflictException>(StatusCode.AlreadyExists, exposeMessage: true));

    [OneTimeTearDown]
    public async Task TearDown() => await server.DisposeAsync();

    private static LookupRequest Request(string name) => new() { Name = name };

    private static RpcException ErrorOf<T>(Result<T> result) => (RpcException)result.GetError();

    private RpcException ServerErrorOf(string name)
    {
        using var call = server.Client.GetAsync(Request(name));

        return Assert.ThrowsAsync<RpcException>(async () => await call.ResponseAsync)!;
    }

    [Test]
    public async Task UnaryCall_ReturnsTheResponse()
    {
        var result = await server.Client.GetAsync(Request("ada")).ToResult();

        Assert.That(result.GetValue().Value, Is.EqualTo("Countess"));
    }

    [Test]
    public async Task UnaryCall_ReturnsTheRpcExceptionAsError()
    {
        var result = await server.Client.GetAsync(Request("rpc")).ToResult();

        var error = ErrorOf(result);
        Assert.That(error.StatusCode, Is.EqualTo(StatusCode.Aborted));
    }

    [Test]
    public async Task ToResult_RelaysADownstreamErrorWithItsStatusCodeOnly()
    {
        var error = ErrorOf(await server.Client.GetAsync(Request("rpc")).ToResult());

        Assert.That(error.StatusCode, Is.EqualTo(StatusCode.Aborted));
        Assert.That(error.Status.Detail, Is.Empty);
        Assert.That(error.Trailers, Is.Empty);
        Assert.That(error.Status.DebugException, Is.TypeOf<RpcException>().With.Property("Status").Property("Detail").EqualTo("Retry later"));
    }

    [Test]
    public async Task Interceptor_KeepsTheDetailAndTrailersOfADeliberateRpcException()
    {
        var error = ServerErrorOf("deliberate");

        Assert.That(error.StatusCode, Is.EqualTo(StatusCode.NotFound));
        Assert.That(error.Status.Detail, Is.EqualTo("Order 5 not found"));
        Assert.That(error.Trailers.Select(trailer => trailer.Key), Does.Contain("hint"));
    }

    [Test]
    public async Task ClientStreamingCall_ReturnsTheResponse()
    {
        using var call = server.Client.Upload();
        await call.RequestStream.WriteAsync(Request("ada"));
        await call.RequestStream.CompleteAsync();

        var result = await call.ToResult();

        Assert.That(result.GetValue().Value, Is.EqualTo("Countess"));
    }

    [Test]
    public async Task ClientStreamingCall_ReturnsTheRpcExceptionAsError()
    {
        using var call = server.Client.Upload();
        await call.RequestStream.WriteAsync(Request("rpc"));
        await call.RequestStream.CompleteAsync();

        var result = await call.ToResult();

        Assert.That(ErrorOf(result).StatusCode, Is.EqualTo(StatusCode.Aborted));
    }

    [Test]
    public void CancelledCall_ThrowsOperationCanceledException()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var exception = Assert.CatchAsync<OperationCanceledException>(
            async () => await server.Client.GetAsync(Request("slow"), cancellationToken: cancellation.Token).ToResult());

        Assert.That(exception!.InnerException, Is.TypeOf<RpcException>().With.Property("StatusCode").EqualTo(StatusCode.Cancelled));
    }

    [Test]
    public void UnaryCall_ThrowsForNullCall() =>
        Assert.ThrowsAsync<ArgumentNullException>(async () => await CallExtensions.ToResult<LookupReply>(null!));

    [Test]
    public void ClientStreamingCall_ThrowsForNullCall() =>
        Assert.ThrowsAsync<ArgumentNullException>(async () => await CallExtensions.ToResult<LookupRequest, LookupReply>(null!));

    [Test]
    public async Task Interceptor_MapsKeyNotFoundToNotFound()
    {
        var error = ErrorOf(await server.Client.GetAsync(Request("missing")).ToResult());

        Assert.That(error.StatusCode, Is.EqualTo(StatusCode.NotFound));
        Assert.That(error.Status.Detail, Is.Empty);
    }

    [Test]
    public async Task Interceptor_MapsValidationErrorsToInvalidArgumentWithMessages()
    {
        var error = ServerErrorOf("invalid");

        Assert.That(error.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        Assert.That(error.Status.Detail, Is.EqualTo("name: Name is too short.; name: Name is reserved."));
    }

    [Test]
    public async Task Interceptor_HidesTheMessageOfAnUnexposedMapping()
    {
        var error = ErrorOf(await server.Client.GetAsync(Request("forbidden")).ToResult());

        Assert.That(error.StatusCode, Is.EqualTo(StatusCode.PermissionDenied));
        Assert.That(error.Status.Detail, Does.Not.Contain("Secret"));
    }

    [Test]
    public async Task Interceptor_UsesACustomMapping()
    {
        var error = ServerErrorOf("custom");

        Assert.That(error.StatusCode, Is.EqualTo(StatusCode.AlreadyExists));
        Assert.That(error.Status.Detail, Is.EqualTo("Name is taken"));
    }

    [Test]
    public void Interceptor_ReportsAnExceptionAfterClientCancellationAsCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var interceptor = new ResultServerInterceptor();

        var error = Assert.ThrowsAsync<RpcException>(async () => await interceptor.UnaryServerHandler<LookupRequest, LookupReply>(
            Request("slow"),
            new CancelledCallContext(cancellation.Token),
            (_, _) => throw new OperationCanceledException()))!;

        Assert.That(error.StatusCode, Is.EqualTo(StatusCode.Cancelled));
    }

    [Test]
    public void Interceptor_DoesNotReportAnotherExceptionAfterClientCancellationAsCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var interceptor = new ResultServerInterceptor();
        var context = new CancelledCallContext(cancellation.Token);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await interceptor.UnaryServerHandler<LookupRequest, LookupReply>(
            Request("slow"), context, (_, _) => throw new InvalidOperationException("Secret internals")));
        var mapped = Assert.ThrowsAsync<RpcException>(async () => await interceptor.UnaryServerHandler<LookupRequest, LookupReply>(
            Request("slow"), context, (_, _) => throw new KeyNotFoundException("Gone")))!;

        Assert.That(mapped.StatusCode, Is.EqualTo(StatusCode.NotFound));
    }

    [Test]
    public async Task Interceptor_LeavesAnUnmappedExceptionToGrpc()
    {
        var error = ErrorOf(await server.Client.GetAsync(Request("other")).ToResult());

        Assert.That(error.StatusCode, Is.EqualTo(StatusCode.Unknown));
        Assert.That(error.Status.Detail, Does.Not.Contain("Secret"));
    }

    [Test]
    public void Interceptor_MapsServerStreamingCalls()
    {
        using var call = server.Client.Watch(Request("missing"));

        var error = Assert.ThrowsAsync<RpcException>(async () => await call.ResponseStream.MoveNext(CancellationToken.None))!;

        Assert.That(error.StatusCode, Is.EqualTo(StatusCode.NotFound));
    }

    [Test]
    public async Task Interceptor_MapsDuplexStreamingCalls()
    {
        using var call = server.Client.Chat();
        await call.RequestStream.WriteAsync(Request("missing"));
        await call.RequestStream.CompleteAsync();

        var error = Assert.ThrowsAsync<RpcException>(async () => await call.ResponseStream.MoveNext(CancellationToken.None))!;

        Assert.That(error.StatusCode, Is.EqualTo(StatusCode.NotFound));
    }

    [Test]
    public async Task Interceptor_MapsClientStreamingCalls()
    {
        using var call = server.Client.Upload();
        await call.RequestStream.WriteAsync(Request("missing"));
        await call.RequestStream.CompleteAsync();

        Assert.That(ErrorOf(await call.ToResult()).StatusCode, Is.EqualTo(StatusCode.NotFound));
    }

    [Test]
    public async Task Interceptor_KeepsSucceedingStreamingCallsWorking()
    {
        using var watch = server.Client.Watch(Request("ada"));

        Assert.That(await watch.ResponseStream.MoveNext(CancellationToken.None), Is.True);
        Assert.That(watch.ResponseStream.Current.Value, Is.EqualTo("Countess"));

        using var chat = server.Client.Chat();
        await chat.RequestStream.WriteAsync(Request("ada"));
        await chat.RequestStream.CompleteAsync();

        Assert.That(await chat.ResponseStream.MoveNext(CancellationToken.None), Is.True);
        Assert.That(chat.ResponseStream.Current.Value, Is.EqualTo("Countess"));
    }
}

internal sealed class CancelledCallContext(CancellationToken cancellationToken) : ServerCallContext
{
    protected override string MethodCore => "/lookup.Lookup/Get";

    protected override string HostCore => "localhost";

    protected override string PeerCore => "test";

    protected override DateTime DeadlineCore => DateTime.MaxValue;

    protected override Metadata RequestHeadersCore { get; } = [];

    protected override CancellationToken CancellationTokenCore => cancellationToken;

    protected override Metadata ResponseTrailersCore { get; } = [];

    protected override Status StatusCore { get; set; }

    protected override WriteOptions? WriteOptionsCore { get; set; }

    protected override AuthContext AuthContextCore { get; } = new(null, []);

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
        throw new NotSupportedException();

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
}
