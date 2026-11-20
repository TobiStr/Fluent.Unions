using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using Refit;

namespace Fluent.Unions.Refit.Tests;

public sealed record User(int Id, string Name);

[JsonSerializable(typeof(User))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class TestJsonContext : JsonSerializerContext;

public interface IUserApi
{
    [Get("/users/{id}")]
    Task<IApiResponse<User>> GetUser(int id, CancellationToken cancellationToken = default);

    [Get("/users/{id}")]
    Task<ApiResponse<User>> GetUserResponse(int id, CancellationToken cancellationToken = default);

    [Delete("/users/{id}")]
    Task<IApiResponse> DeleteUser(int id, CancellationToken cancellationToken = default);
}

internal sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await respond(request, cancellationToken);
        response.RequestMessage = request;
        return response;
    }

    public static IUserApi Api(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) =>
        RestService.ForGenerated<IUserApi>(
            new HttpClient(new FakeHandler(respond)) { BaseAddress = new Uri("http://localhost") },
            RefitSettings.ForJsonContext(TestJsonContext.Default));

    public static IUserApi Api(HttpStatusCode status, string? json = null) =>
        Api((_, _) => Task.FromResult(Reply(status, json)));

    public static HttpResponseMessage Reply(HttpStatusCode status, string? json = null)
    {
        var response = new HttpResponseMessage(status);

        if (json is not null)
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");

        return response;
    }
}
