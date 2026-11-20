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
        Result result => result.ToHttpResult(),
        IUnion union when Is(returned, typeof(Result<>)) => union.Value switch
        {
            Exception error => new ErrorHttpResult(error),
            null => new ErrorHttpResult(DefaultResult(returned.GetType())),
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
        null => new ErrorHttpResult(DefaultResult(returned.GetType())),
        Exception error when shape.ErrorType.IsInstanceOfType(error) => new ErrorHttpResult(error),
        { } error when shape.ErrorType.IsInstanceOfType(error) => new ErrorHttpResult(new ResultErrorException(error)),
        var value => TypedResults.Ok(value)
    };

    private static InvalidOperationException DefaultResult(Type type) =>
        new($"The endpoint returned a default {type.Name} that holds neither a value nor an error.");

    internal static ResultShape? ShapeOf(Type type) => Shapes.GetOrAdd(type, static candidate =>
    {
        if (!candidate.IsValueType)
            return null;

        foreach (var contract in candidate.GetInterfaces())
        {
            if (!contract.IsGenericType || contract.GetGenericTypeDefinition() != typeof(IResult<,,>))
                continue;

            var arguments = contract.GetGenericArguments();
            var errorResult = typeof(IErrorResult<,>).MakeGenericType(arguments[0], arguments[2]);

            return new ResultShape(arguments[1], arguments[2], errorResult.GetProperty(nameof(IErrorResult<,>.Value))!);
        }

        return null;
    });

    internal sealed record ResultShape(Type ValueType, Type ErrorType, PropertyInfo Value);

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
    /// <see cref="Result"/>, 404 for <see cref="Option{T}"/>, and ProblemDetails for errors.
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
        if (returnType == typeof(Result))
        {
            metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status204NoContent, typeof(void)));
            AddErrorMetadata(metadata);
        }
        else if (ResultEndpointFilter.Is(returnType, typeof(Result<>)))
        {
            metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, returnType.GetGenericArguments()[0], Json));
            AddErrorMetadata(metadata);
        }
        else if (ResultEndpointFilter.Is(returnType, typeof(Option<>)))
        {
            metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, returnType.GetGenericArguments()[0], Json));
            metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status404NotFound, typeof(void)));
        }
        else if (ResultEndpointFilter.ShapeOf(returnType) is { } shape)
        {
            metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, shape.ValueType, Json));
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
