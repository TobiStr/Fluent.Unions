using Fluent.Unions.Grpc.Tests.Protos;
using Fluent.Unions.Validation;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Grpc.Tests.Scenarios;

public sealed class NameReservedException(string message) : Exception(message);

public sealed class NicknameService : Lookup.LookupBase
{
    private static readonly Dictionary<string, string> Nicknames = new() { ["ada"] = "Countess", ["grace"] = "Amazing Grace" };

    public override Task<LookupReply> Get(LookupRequest request, ServerCallContext context) =>
        Task.FromResult(Find(request.Name).Check().GetValue());

    private static Result<LookupReply> Find(string name) =>
        name.EnsureLongerThanOrEqual(3, name: "name")
            .Ensure(candidate => candidate != "admin", _ => new NameReservedException("The name 'admin' is reserved."))
            .Ensure(candidate => candidate != "boom", _ => new InvalidOperationException("Secret internals"))
            .Chain<string, LookupReply>(Lookup);

    private static Result<LookupReply> Lookup(string name) =>
        Nicknames.TryGetValue(name, out var nickname)
            ? new LookupReply { Value = nickname }
            : new KeyNotFoundException($"No nickname for '{name}'.");
}

internal sealed class NicknameClient(Lookup.LookupClient client)
{
    public async Task<Result<Option<string>>> Find(string name) =>
        (await client.GetAsync(new LookupRequest { Name = name }).ToResult())
            .Select(reply => Option.Some(reply.Value))
            .Catch<Option<string>, RpcException>(error => error.StatusCode switch
            {
                StatusCode.NotFound => Result<Option<string>>.Ok(Option.None),
                StatusCode.InvalidArgument => new ValidationError(DownstreamDetail(error)),
                StatusCode.FailedPrecondition => new NameReservedException(DownstreamDetail(error)),
                _ => error
            });

    private static string DownstreamDetail(RpcException error) => ((RpcException)error.Status.DebugException!).Status.Detail;
}

/// <summary>
/// A missing nickname is data (<see cref="Option{T}"/>) and every other failure is a typed error, so the caller has one exhaustive place to decide.
/// </summary>
[TestFixture]
public class NicknameDirectoryScenario
{
    private WebApplication _app = null!;
    private GrpcChannel _channel = null!;
    private NicknameClient _client = null!;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddGrpc(grpc => grpc.Interceptors.Add<ResultServerInterceptor>());
        builder.Services.Configure<ResultGrpcOptions>(options => options.Map<NameReservedException>(StatusCode.FailedPrecondition, exposeMessage: true));
        _app = builder.Build();
        _app.MapGrpcService<NicknameService>();
        await _app.StartAsync();
        _channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = _app.GetTestServer().CreateHandler() });
        _client = new NicknameClient(new Lookup.LookupClient(_channel));
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        _channel.Dispose();
        await _app.DisposeAsync();
    }

    private async Task<string> Describe(string name) =>
        (await _client.Find(name)).Match(
            nickname => nickname.Match(value => $"nickname {value}", () => "no nickname"),
            error => error switch
            {
                ValidationError => $"invalid: {error.Message}",
                NameReservedException => $"reserved: {error.Message}",
                _ => "failed"
            });

    [Test]
    public async Task KnownName_ReturnsTheNickname() =>
        Assert.That(await Describe("ada"), Is.EqualTo("nickname Countess"));

    [Test]
    public async Task NotFound_IsRecoveredIntoNoneAndIsNotAnError()
    {
        var result = await _client.Find("nobody");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.GetValue().HasValue, Is.False);
    }

    [Test]
    public async Task ValidationFailureOnTheServer_ArrivesAsAValidationErrorWithItsMessage() =>
        Assert.That(await Describe("al"), Is.EqualTo("invalid: name: name must be at least 3 characters long."));

    [Test]
    public async Task DomainError_IsMappedByTheInterceptorAndRebuiltByTheClient() =>
        Assert.That(await Describe("admin"), Is.EqualTo("reserved: The name 'admin' is reserved."));

    [Test]
    public async Task UnexpectedFailure_StaysAnRpcErrorWithoutLeakingInternals()
    {
        var result = await _client.Find("boom");

        var error = (RpcException)result.GetError();
        Assert.That(error.StatusCode, Is.EqualTo(StatusCode.Unknown));
        Assert.That(error.Message, Does.Not.Contain("Secret"));
        Assert.That(await Describe("boom"), Is.EqualTo("failed"));
    }
}
