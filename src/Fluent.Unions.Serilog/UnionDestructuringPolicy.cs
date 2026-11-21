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
        if (ResultTypes.IsResult(type))
        {
            if (!((IErrorResult)value).IsSuccess)
            {
                result = Error(((IErrorResult)value).Error!, propertyValueFactory);
                return true;
            }

            result = ResultTypes.ValueType(type) is null ? new ScalarValue("Ok") : propertyValueFactory.CreatePropertyValue(union.Value, true);
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
