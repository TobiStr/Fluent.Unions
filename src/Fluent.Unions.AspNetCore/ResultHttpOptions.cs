using Microsoft.AspNetCore.Http;

namespace Fluent.Unions.AspNetCore;

/// <summary>
/// Maps result errors to HTTP status codes. Configure it with <c>services.Configure&lt;ResultHttpOptions&gt;(...)</c>.
/// Unmapped errors give 500 without details.
/// </summary>
public sealed class ResultHttpOptions
{
    private readonly Dictionary<Type, ErrorMapping> mappings = new()
    {
        [typeof(ValidationError)] = new(StatusCodes.Status400BadRequest, true),
        [typeof(ValidationErrors)] = new(StatusCodes.Status400BadRequest, true),
        [typeof(KeyNotFoundException)] = new(StatusCodes.Status404NotFound, false),
        [typeof(UnauthorizedAccessException)] = new(StatusCodes.Status403Forbidden, false),
        [typeof(TimeoutException)] = new(StatusCodes.Status504GatewayTimeout, false)
    };

    /// <summary>
    /// Maps errors of type <typeparamref name="TException"/> and its subclasses to <paramref name="statusCode"/>.
    /// The most specific mapping wins. The exception message reaches the client only with <paramref name="exposeMessage"/>;
    /// validation errors always list their messages.
    /// </summary>
    public ResultHttpOptions Map<TException>(int statusCode, bool exposeMessage = false) where TException : Exception
    {
        mappings[typeof(TException)] = new ErrorMapping(statusCode, exposeMessage);
        return this;
    }

    internal ErrorMapping Resolve(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);

        for (var type = error.GetType(); type is not null; type = type.BaseType)
        {
            if (mappings.TryGetValue(type, out var mapping))
                return mapping;
        }

        return new ErrorMapping(StatusCodes.Status500InternalServerError, false);
    }

    internal readonly record struct ErrorMapping(int StatusCode, bool ExposeMessage);
}
