using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.AspNetCore;

/// <summary>
/// Turns a <see cref="Result"/>, <see cref="Result{T}"/>, <see cref="Option{T}"/> or any
/// <see cref="IResult{TSelf, T, TError}"/> returned by an endpoint into an HTTP response, like <c>ToHttpResult()</c>.
/// An error that is not an exception is wrapped in a <see cref="ResultErrorException"/>. Other return values pass through.
/// </summary>
public sealed class ResultEndpointFilter : IEndpointFilter
{
    private static readonly ConcurrentDictionary<Type, ResultShape?> Shapes = new();

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
        not null when ShapeOf(returned.GetType()) is { } shape => FromCustomResult(returned, shape),
        _ => returned
    };

    private static object FromCustomResult(object returned, ResultShape shape) => shape.Value.GetValue(returned) switch
    {
        null => TypedResults.NotFound(),
        Exception error when shape.ErrorType.IsInstanceOfType(error) => new ErrorHttpResult(error),
        { } error when shape.ErrorType.IsInstanceOfType(error) => new ErrorHttpResult(new ResultErrorException(error)),
        var value => TypedResults.Ok(value)
    };

    private static ResultShape? ShapeOf(Type type) => Shapes.GetOrAdd(type, static candidate =>
    {
        if (!candidate.IsValueType)
            return null;

        foreach (var contract in candidate.GetInterfaces())
        {
            if (!contract.IsGenericType || contract.GetGenericTypeDefinition() != typeof(IResult<,,>))
                continue;

            var arguments = contract.GetGenericArguments();
            var errorResult = typeof(IErrorResult<,>).MakeGenericType(arguments[0], arguments[2]);

            return new ResultShape(arguments[2], errorResult.GetProperty(nameof(IErrorResult<,>.Value))!);
        }

        return null;
    });

    private sealed record ResultShape(Type ErrorType, PropertyInfo Value);

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
