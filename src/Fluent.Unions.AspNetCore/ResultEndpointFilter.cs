using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Fluent.Unions.AspNetCore;

/// <summary>
/// Turns a <see cref="Result"/>, <see cref="Result{T}"/> or <see cref="Option{T}"/> returned by an endpoint into an
/// HTTP response, like <c>ToHttpResult()</c>. Other return values pass through.
/// </summary>
public sealed class ResultEndpointFilter : IEndpointFilter
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return ToHttpResult(await next(context).ConfigureAwait(false));
    }

    private static object? ToHttpResult(object? returned) => returned switch
    {
        Result result => result.ToHttpResult(),
        IUnion union when Is(returned, typeof(Result<>)) => union.Value switch
        {
            Exception error => new ErrorHttpResult(error),
            null => TypedResults.NotFound(),
            var value => TypedResults.Ok(value)
        },
        IUnion union when Is(returned, typeof(Option<>)) => union.Value is null or None
            ? TypedResults.NotFound()
            : TypedResults.Ok(union.Value),
        _ => returned
    };

    private static bool Is(object value, Type genericDefinition) =>
        value.GetType() is { IsGenericType: true } type && type.GetGenericTypeDefinition() == genericDefinition;
}

/// <summary>
/// Registers <see cref="ResultEndpointFilter"/> on endpoints.
/// </summary>
public static class ResultEndpointFilterExtensions
{
    /// <summary>
    /// Lets the endpoints return results and options directly. Works on a single endpoint or a route group.
    /// </summary>
    public static TBuilder WithResultMapping<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(new ResultEndpointFilter());
}
