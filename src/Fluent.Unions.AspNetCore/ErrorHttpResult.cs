using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fluent.Unions.AspNetCore;

/// <summary>
/// Writes an error as ProblemDetails with the status code from <see cref="ResultHttpOptions"/>. Validation errors,
/// also as the inner exception of the error, become a ValidationProblemDetails grouped by member; other messages
/// are exposed only when mapped to be. Responses with a status of 500 or more are logged.
/// </summary>
internal sealed class ErrorHttpResult(Exception error) : IResult
{
    internal static ErrorHttpResult ForEmpty(Type resultType) =>
        new(new InvalidOperationException($"The endpoint returned a default {resultType.Name} that holds neither a value nor an error."));

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var options = httpContext.RequestServices.GetService<IOptions<ResultHttpOptions>>()?.Value ?? new ResultHttpOptions();
        var source = ValidationSource(error);
        var mapping = options.Resolve(source);

        if (mapping.StatusCode >= StatusCodes.Status500InternalServerError)
            httpContext.RequestServices.GetService<ILoggerFactory>()?
                .CreateLogger("Fluent.Unions.AspNetCore")
                .LogError(error, "A result error was turned into a {StatusCode} response.", mapping.StatusCode);

        var result = source switch
        {
            ValidationErrors errors => Results.ValidationProblem(errors.ToDictionary(), statusCode: mapping.StatusCode),
            ValidationError single => Results.ValidationProblem(
                new Dictionary<string, string[]> { [single.MemberName ?? ""] = [single.Message] },
                statusCode: mapping.StatusCode),
            _ => Results.Problem(detail: mapping.ExposeMessage ? source.Message : null, statusCode: mapping.StatusCode)
        };

        return result.ExecuteAsync(httpContext);
    }

    private static Exception ValidationSource(Exception exception) =>
        exception is not (ValidationError or ValidationErrors or AggregateException) && exception.InnerException is ValidationError or ValidationErrors
            ? exception.InnerException
            : exception;
}
