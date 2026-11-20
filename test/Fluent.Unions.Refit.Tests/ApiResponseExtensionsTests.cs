using System.Net;
using NUnit.Framework;
using Refit;

namespace Fluent.Unions.Refit.Tests;

public class ApiResponseExtensionsTests
{
    private const string UserJson = """{"id":7,"name":"Ada"}""";

    [Test]
    public async Task ToResult_OfSuccessfulResponse_HoldsTheContent()
    {
        var api = FakeHandler.Api(HttpStatusCode.OK, UserJson);

        var result = (await api.GetUser(7)).ToResult();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.GetValue(), Is.EqualTo(new User(7, "Ada")));
    }

    [Test]
    public async Task ToResult_OfErrorStatus_HoldsTheApiException()
    {
        var api = FakeHandler.Api(HttpStatusCode.InternalServerError, """{"title":"boom"}""");

        var result = (await api.GetUser(7)).ToResult();

        var error = result.GetError();
        Assert.That(error, Is.TypeOf<ApiException>());
        var apiException = (ApiException)error;
        Assert.That(apiException.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(apiException.Content, Does.Contain("boom"));
    }

    [Test]
    public async Task ToResult_OfNetworkFailure_HoldsTheRequestExceptionWithTheHttpRequestException()
    {
        var api = FakeHandler.Api((_, _) => throw new HttpRequestException("no route to host"));

        var result = (await api.GetUser(7)).ToResult();

        var error = result.GetError();
        Assert.That(error, Is.TypeOf<ApiRequestException>());
        Assert.That(error.InnerException, Is.TypeOf<HttpRequestException>());
    }

    [Test]
    public async Task ToResult_OfNullContent_IsAnError()
    {
        var api = FakeHandler.Api(HttpStatusCode.OK, "null");

        var result = (await api.GetUser(7)).ToResult();

        Assert.That(result.GetError(), Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task ToResult_OfTimeout_ThrowsInsteadOfReturningAnError()
    {
        var api = FakeHandler.Api((_, _) => throw new TaskCanceledException("timed out"));

        var response = await api.GetUser(7);

        Assert.Throws<TaskCanceledException>(() => response.ToResult());
    }

    [Test]
    public async Task ToResult_WithoutContent_IsOkOnSuccessAndErrorOnFailure()
    {
        var ok = await FakeHandler.Api(HttpStatusCode.NoContent).DeleteUser(7);
        var failed = await FakeHandler.Api(HttpStatusCode.Forbidden).DeleteUser(7);

        Assert.That(ok.ToResult().IsSuccess, Is.True);
        Assert.That(failed.ToResult().GetError(), Is.TypeOf<ApiException>());
    }

    [Test]
    public async Task ToResult_WithoutContent_OfTimeout_Throws()
    {
        var api = FakeHandler.Api((_, _) => throw new TaskCanceledException("timed out"));

        var response = await api.DeleteUser(7);

        Assert.Throws<TaskCanceledException>(() => response.ToResult());
    }

    [Test]
    public async Task ToOptionalResult_OfSuccessfulResponse_HoldsTheContent()
    {
        var api = FakeHandler.Api(HttpStatusCode.OK, UserJson);

        var result = (await api.GetUser(7)).ToOptionalResult();

        Assert.That(result.GetValue().GetValue(), Is.EqualTo(new User(7, "Ada")));
    }

    [Test]
    public async Task ToOptionalResult_OfNotFound_IsNone()
    {
        var api = FakeHandler.Api(HttpStatusCode.NotFound);

        var result = (await api.GetUser(7)).ToOptionalResult();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.GetValue().HasValue, Is.False);
    }

    [Test]
    public async Task ToOptionalResult_OfNullContent_IsNone()
    {
        var api = FakeHandler.Api(HttpStatusCode.OK, "null");

        var result = (await api.GetUser(7)).ToOptionalResult();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.GetValue().HasValue, Is.False);
    }

    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.InternalServerError)]
    public async Task ToOptionalResult_OfOtherFailure_IsAnError(HttpStatusCode status)
    {
        var api = FakeHandler.Api(status);

        var result = (await api.GetUser(7)).ToOptionalResult();

        Assert.That(result.GetError(), Is.TypeOf<ApiException>());
    }

    [Test]
    public async Task ToOptionalResult_OfNetworkFailure_IsAnError()
    {
        var api = FakeHandler.Api((_, _) => throw new HttpRequestException("no route to host"));

        var result = (await api.GetUser(7)).ToOptionalResult();

        Assert.That(result.GetError(), Is.TypeOf<ApiRequestException>());
    }

    [Test]
    public async Task ToResult_OfInterfaceResponse_ConvertsTheResponse()
    {
        var ok = await FakeHandler.Api(HttpStatusCode.OK, UserJson).GetUser(7).ToResult();
        var failed = await FakeHandler.Api(HttpStatusCode.BadRequest).GetUser(7).ToResult();

        Assert.That(ok.GetValue(), Is.EqualTo(new User(7, "Ada")));
        Assert.That(failed.GetError(), Is.TypeOf<ApiException>());
    }

    [Test]
    public async Task ToResult_OfConcreteResponse_ConvertsTheResponse()
    {
        var ok = await FakeHandler.Api(HttpStatusCode.OK, UserJson).GetUserResponse(7).ToResult();
        var failed = await FakeHandler.Api(HttpStatusCode.BadRequest).GetUserResponse(7).ToResult();

        Assert.That(ok.GetValue(), Is.EqualTo(new User(7, "Ada")));
        Assert.That(failed.GetError(), Is.TypeOf<ApiException>());
    }

    [Test]
    public async Task ToResult_WithoutContent_ConvertsTheResponse()
    {
        var ok = await FakeHandler.Api(HttpStatusCode.NoContent).DeleteUser(7).ToResult();
        var failed = await FakeHandler.Api(HttpStatusCode.Conflict).DeleteUser(7).ToResult();

        Assert.That(ok.IsSuccess, Is.True);
        Assert.That(failed.GetError(), Is.TypeOf<ApiException>());
    }

    [Test]
    public async Task ToOptionalResult_OfInterfaceResponse_ConvertsTheResponse()
    {
        var found = await FakeHandler.Api(HttpStatusCode.OK, UserJson).GetUser(7).ToOptionalResult();
        var missing = await FakeHandler.Api(HttpStatusCode.NotFound).GetUser(7).ToOptionalResult();

        Assert.That(found.GetValue().GetValue(), Is.EqualTo(new User(7, "Ada")));
        Assert.That(missing.GetValue().HasValue, Is.False);
    }

    [Test]
    public async Task ToOptionalResult_OfConcreteResponse_ConvertsTheResponse()
    {
        var found = await FakeHandler.Api(HttpStatusCode.OK, UserJson).GetUserResponse(7).ToOptionalResult();
        var missing = await FakeHandler.Api(HttpStatusCode.NotFound).GetUserResponse(7).ToOptionalResult();

        Assert.That(found.GetValue().GetValue(), Is.EqualTo(new User(7, "Ada")));
        Assert.That(missing.GetValue().HasValue, Is.False);
    }

    [Test]
    public void ToResult_OfCancelledCall_Throws()
    {
        using var cts = new CancellationTokenSource();
        var api = FakeHandler.Api((_, ct) =>
        {
            cts.Cancel();
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(FakeHandler.Reply(HttpStatusCode.OK, UserJson));
        });

        Assert.CatchAsync<OperationCanceledException>(() => api.GetUser(7, cts.Token).ToResult());
    }

    [Test]
    public void ToResult_OfAlreadyCancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var api = FakeHandler.Api(HttpStatusCode.OK, UserJson);

        Assert.CatchAsync<OperationCanceledException>(() => api.GetUser(7, cts.Token).ToResult());
    }

    [Test]
    public void Conversions_RejectNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((IApiResponse)null!).ToResult());
        Assert.Throws<ArgumentNullException>(() => ((IApiResponse<User>)null!).ToResult());
        Assert.Throws<ArgumentNullException>(() => ((IApiResponse<User>)null!).ToOptionalResult());
        Assert.ThrowsAsync<ArgumentNullException>(() => ((Task<IApiResponse>)null!).ToResult());
        Assert.ThrowsAsync<ArgumentNullException>(() => ((Task<IApiResponse<User>>)null!).ToResult());
        Assert.ThrowsAsync<ArgumentNullException>(() => ((Task<ApiResponse<User>>)null!).ToResult());
        Assert.ThrowsAsync<ArgumentNullException>(() => ((Task<IApiResponse<User>>)null!).ToOptionalResult());
        Assert.ThrowsAsync<ArgumentNullException>(() => ((Task<ApiResponse<User>>)null!).ToOptionalResult());
    }
}
