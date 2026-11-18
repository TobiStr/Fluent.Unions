using Microsoft.AspNetCore.Http;

namespace ResultType.AspNetCore;

/// <summary>
/// Maps result errors to HTTP status codes. Configure it with <c>services.Configure&lt;ResultHttpOptions&gt;(...)</c>.
/// </summary>
public sealed class ResultHttpOptions
{
    private readonly Dictionary<Type, int> statusCodes = new()
    {
        [typeof(ArgumentException)] = StatusCodes.Status400BadRequest,
        [typeof(AggregateException)] = StatusCodes.Status400BadRequest,
        [typeof(KeyNotFoundException)] = StatusCodes.Status404NotFound,
        [typeof(UnauthorizedAccessException)] = StatusCodes.Status403Forbidden,
        [typeof(TimeoutException)] = StatusCodes.Status504GatewayTimeout
    };

    /// <summary>
    /// Maps errors of type <typeparamref name="TException"/> and its subclasses to <paramref name="statusCode"/>.
    /// The most specific mapping wins; unmapped errors give 500.
    /// </summary>
    public ResultHttpOptions Map<TException>(int statusCode) where TException : Exception
    {
        statusCodes[typeof(TException)] = statusCode;
        return this;
    }

    /// <summary>
    /// Returns the status code for <paramref name="error"/>.
    /// </summary>
    public int GetStatusCode(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);

        for (var type = error.GetType(); type is not null; type = type.BaseType)
        {
            if (statusCodes.TryGetValue(type, out var statusCode))
                return statusCode;
        }

        return StatusCodes.Status500InternalServerError;
    }
}
