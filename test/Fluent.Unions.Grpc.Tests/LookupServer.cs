using Fluent.Unions.Grpc.Tests.Protos;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Fluent.Unions.Grpc.Tests;

public sealed class LookupServer : IAsyncDisposable
{
    private readonly WebApplication app;
    private readonly GrpcChannel channel;

    private LookupServer(WebApplication app, GrpcChannel channel)
    {
        this.app = app;
        this.channel = channel;
        Client = new Lookup.LookupClient(channel);
    }

    public Lookup.LookupClient Client { get; }

    public static async Task<LookupServer> StartAsync(ResultGrpcOptions? options = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddGrpc(grpc => grpc.Interceptors.Add<ResultServerInterceptor>());
        builder.Services.AddSingleton(options ?? new ResultGrpcOptions());

        var app = builder.Build();
        app.MapGrpcService<LookupService>();
        await app.StartAsync();

        var channel = GrpcChannel.ForAddress(
            "http://localhost",
            new GrpcChannelOptions { HttpHandler = app.GetTestServer().CreateHandler() });

        return new LookupServer(app, channel);
    }

    public async ValueTask DisposeAsync()
    {
        channel.Dispose();
        await app.DisposeAsync();
    }
}
