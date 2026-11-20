using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fluent.Unions.AspNetCore;

/// <summary>
/// Writes an error as ProblemDetails with the status code from <see cref="ResultHttpOptions"/>. Validation errors
/// become a ValidationProblemDetails grouped by member; other messages are exposed only when mapped to be.
/// </summary>
internal sealed class ErrorHttpResult(Exception error) : IResult
{
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var options = httpContext.RequestServices.GetService<IOptions<ResultHttpOptions>>()?.Value ?? new ResultHttpOptions();
        var mapping = options.Resolve(error);

        var result = error switch
        {
            ValidationErrors errors => Results.ValidationProblem(errors.ToDictionary(), statusCode: mapping.StatusCode),
            ValidationError single => Results.ValidationProblem(
                new Dictionary<string, string[]> { [single.MemberName ?? ""] = [single.Message] },
                statusCode: mapping.StatusCode),
            _ => Results.Problem(detail: mapping.ExposeMessage ? error.Message : null, statusCode: mapping.StatusCode)
        };

        return result.ExecuteAsync(httpContext);
    }
}
