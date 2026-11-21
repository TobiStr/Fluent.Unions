using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Fluent.Unions.Abstractions;
using Serilog.Core;
using Serilog.Events;

namespace Fluent.Unions.Serilog;

/// <summary>
/// Destructures every result type, <see cref="Option{T}"/> and <see cref="Loadable{T}"/> into structured log values.
/// </summary>
public sealed class UnionDestructuringPolicy : IDestructuringPolicy
{
    private const string ErrorProperty = "Error";
    private const string TypeProperty = "Type";
    private const string MessageProperty = "Message";

    /// <inheritdoc />
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, [NotNullWhen(true)] out LogEventPropertyValue? result)
    {
        result = null;

        if (value is not IUnion union)
        {
            return false;
        }

        var type = value.GetType();
        if (ResultTypes.ErrorType(type) is { } errorType)
        {
            if (ResultTypes.ValueType(type) is null && union.Value is Ok)
            {
                result = new ScalarValue("Ok");
                return true;
            }

            var inner = union.Value ?? GetError(value);
            result = errorType.IsInstanceOfType(inner) ? Error(inner, propertyValueFactory) : propertyValueFactory.CreatePropertyValue(inner, true);
            return true;
        }

        if (!type.IsGenericType)
        {
            return false;
        }

        var definition = type.GetGenericTypeDefinition();
        if (definition == typeof(Option<>))
        {
            result = union.Value is null or None ? new ScalarValue(null) : propertyValueFactory.CreatePropertyValue(union.Value, true);
            return true;
        }

        if (definition == typeof(Loadable<>))
        {
            result = union.Value switch
            {
                null or NotLoaded => new ScalarValue(nameof(NotLoaded)),
                Loading => new ScalarValue(nameof(Loading)),
                Exception error => Error(error, propertyValueFactory),
                var loaded => propertyValueFactory.CreatePropertyValue(loaded, true),
            };
            return true;
        }

        return false;
    }

    private static object GetError(object result) => result.GetType().GetMethod(nameof(Result.GetError))!.Invoke(result, null)!;

    private static StructureValue Error(object error, ILogEventPropertyValueFactory propertyValueFactory)
    {
        LogEventPropertyValue described = error is Exception exception
            ? new StructureValue(
            [
                new LogEventProperty(TypeProperty, new ScalarValue(exception.GetType().FullName)),
                new LogEventProperty(MessageProperty, new ScalarValue(exception.Message)),
            ])
            : propertyValueFactory.CreatePropertyValue(error, true);

        return new StructureValue([new LogEventProperty(ErrorProperty, described)]);
    }
}
