using Grpc.Core;

namespace Fluent.Unions.Grpc;

/// <summary>
/// Maps result errors to gRPC status codes. Unmapped errors give <see cref="StatusCode.Internal"/> without details.
/// </summary>
public sealed class ResultGrpcOptions
{
    private readonly Dictionary<Type, ErrorMapping> mappings = new()
    {
        [typeof(ValidationError)] = new(StatusCode.InvalidArgument, true),
        [typeof(ValidationErrors)] = new(StatusCode.InvalidArgument, true),
        [typeof(KeyNotFoundException)] = new(StatusCode.NotFound, false),
        [typeof(UnauthorizedAccessException)] = new(StatusCode.PermissionDenied, false),
        [typeof(TimeoutException)] = new(StatusCode.DeadlineExceeded, false)
    };

    /// <summary>
    /// Maps errors of type <typeparamref name="TException"/> and its subclasses to <paramref name="statusCode"/>.
    /// The most specific mapping wins. The exception message reaches the client only with <paramref name="exposeMessage"/>;
    /// validation errors always list their messages.
    /// </summary>
    public ResultGrpcOptions Map<TException>(StatusCode statusCode, bool exposeMessage = false) where TException : Exception
    {
        mappings[typeof(TException)] = new ErrorMapping(statusCode, exposeMessage);
        return this;
    }

    internal bool TryResolve(Exception error, out ErrorMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(error);

        for (var type = error.GetType(); type is not null; type = type.BaseType)
        {
            if (mappings.TryGetValue(type, out mapping))
                return true;
        }

        mapping = default;
        return false;
    }

    internal readonly record struct ErrorMapping(StatusCode StatusCode, bool ExposeMessage);
}
