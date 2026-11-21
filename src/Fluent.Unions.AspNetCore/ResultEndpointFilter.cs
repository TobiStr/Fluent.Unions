using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
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
        IUnion union when Is(returned, typeof(Option<>)) => union.Value is null or None
            ? TypedResults.NotFound()
            : TypedResults.Ok(union.Value),
        not null when ShapeOf(returned.GetType()) is { } shape => FromResult(returned, shape),
        _ => returned
    };

    private static object FromResult(object returned, ResultShape shape) => shape.ReadValue(returned) switch
    {
        null => ErrorHttpResult.ForEmpty(returned.GetType()),
        Exception error when shape.ErrorType.IsInstanceOfType(error) => new ErrorHttpResult(error),
        { } error when shape.ErrorType.IsInstanceOfType(error) => new ErrorHttpResult(new ResultErrorException(error)),
        Ok when shape.ValueType is null => TypedResults.NoContent(),
        var value => TypedResults.Ok(value)
    };

    internal static ResultShape? ShapeOf(Type type) => Shapes.GetOrAdd(type, static candidate =>
        ResultTypes.ErrorType(candidate) is { } errorType
            ? new ResultShape(
                ResultTypes.ValueType(candidate),
                errorType,
                (Func<object, object?>)typeof(ResultEndpointFilter)
                    .GetMethod(nameof(Read), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(candidate, errorType)
                    .CreateDelegate(typeof(Func<object, object?>)))
            : null);

    private static object? Read<TSelf, TError>(object result)
        where TSelf : struct, IErrorResult<TSelf, TError>
        where TError : notnull =>
        ((TSelf)result).Value;

    internal sealed record ResultShape(Type? ValueType, Type ErrorType, Func<object, object?> ReadValue);

    internal static bool Is(object value, Type genericDefinition) => Is(value.GetType(), genericDefinition);

    internal static bool Is(Type type, Type genericDefinition) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == genericDefinition;
}

/// <summary>
/// Registers <see cref="ResultEndpointFilter"/> on endpoints.
/// </summary>
public static class ResultEndpointFilterExtensions
{
    private static readonly string[] Json = ["application/json"];
    private static readonly string[] ProblemJson = ["application/problem+json"];

    /// <summary>
    /// Lets the endpoints return results and options directly. Works on a single endpoint or a route group.
    /// The OpenAPI response types are taken from the handler's return type: the value type for 200, or 204 for
    /// <see cref="Result"/>, 404 for <see cref="Option{T}"/>, and ProblemDetails for errors. Errors are described as
    /// 400 and 500 only. Add <c>.Produces</c> for the other status codes you map.
    /// </summary>
    public static TBuilder WithResultMapping<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add(endpoint =>
        {
            var handler = endpoint.Metadata.OfType<MethodInfo>().FirstOrDefault();
            if (handler is not null)
                AddResponseMetadata(endpoint.Metadata, UnwrapTask(handler.ReturnType));
        });

        return builder.AddEndpointFilter(new ResultEndpointFilter());
    }

    private static void AddResponseMetadata(IList<object> metadata, Type returnType)
    {
        if (ResultEndpointFilter.Is(returnType, typeof(Option<>)))
        {
            metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, returnType.GetGenericArguments()[0], Json));
            metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status404NotFound, typeof(void)));
        }
        else if (ResultEndpointFilter.ShapeOf(returnType) is { } shape)
        {
            metadata.Add(shape.ValueType is null
                ? new ProducesResponseTypeMetadata(StatusCodes.Status204NoContent, typeof(void))
                : new ProducesResponseTypeMetadata(StatusCodes.Status200OK, shape.ValueType, Json));
            AddErrorMetadata(metadata);
        }
    }

    private static void AddErrorMetadata(IList<object> metadata)
    {
        metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest, typeof(HttpValidationProblemDetails), ProblemJson));
        metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status500InternalServerError, typeof(ProblemDetails), ProblemJson));
    }

    private static Type UnwrapTask(Type type) =>
        type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(ValueTask<>))
            ? type.GetGenericArguments()[0]
            : type;
}
