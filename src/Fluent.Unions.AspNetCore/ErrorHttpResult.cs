using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fluent.Unions.AspNetCore;

/// <summary>
/// Writes an error as ProblemDetails, with the status code from <see cref="ResultHttpOptions"/>.
/// The exception message is only exposed for client errors (4xx).
/// </summary>
internal sealed class ErrorHttpResult(Exception error) : IResult
{
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var options = httpContext.RequestServices.GetService<IOptions<ResultHttpOptions>>()?.Value ?? new ResultHttpOptions();
        var statusCode = options.GetStatusCode(error);
        var isClientError = statusCode is >= 400 and < 500;
        var extensions = isClientError && error is AggregateException aggregate
            ? new Dictionary<string, object?> { ["errors"] = aggregate.InnerExceptions.Select(e => e.Message).ToArray() }
            : null;

        return TypedResults.Problem(
                detail: isClientError ? error.Message : null,
                statusCode: statusCode,
                extensions: extensions)
            .ExecuteAsync(httpContext);
    }
}
