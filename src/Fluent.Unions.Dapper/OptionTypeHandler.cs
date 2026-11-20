using System.ComponentModel;
using System.Data;
using System.Globalization;
using Dapper;

namespace Fluent.Unions.Dapper;

/// <summary>
/// Maps an <see cref="Option{T}"/> to a parameter or column: no value is <c>NULL</c>.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public sealed class OptionTypeHandler<T> : SqlMapper.TypeHandler<Option<T>>
{
    private static readonly Type ValueType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

    private static readonly Dictionary<Type, DbType> DbTypes = new()
    {
        [typeof(bool)] = DbType.Boolean,
        [typeof(byte)] = DbType.Byte,
        [typeof(short)] = DbType.Int16,
        [typeof(int)] = DbType.Int32,
        [typeof(long)] = DbType.Int64,
        [typeof(float)] = DbType.Single,
        [typeof(double)] = DbType.Double,
        [typeof(decimal)] = DbType.Decimal,
        [typeof(string)] = DbType.String,
        [typeof(Guid)] = DbType.Guid,
        [typeof(DateTime)] = DbType.DateTime,
        [typeof(DateTimeOffset)] = DbType.DateTimeOffset,
        [typeof(TimeSpan)] = DbType.Time,
        [typeof(byte[])] = DbType.Binary,
    };

    private static readonly DbType? ParameterDbType =
        DbTypes.TryGetValue(ValueType.IsEnum ? Enum.GetUnderlyingType(ValueType) : ValueType, out var dbType) ? dbType : null;

    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, Option<T> value)
    {
        if (ParameterDbType is { } parameterDbType)
        {
            parameter.DbType = parameterDbType;
        }

        parameter.Value = value.TryGetValue(out var payload) ? ToParameterValue(payload) : DBNull.Value;
    }

    /// <inheritdoc />
    public override Option<T> Parse(object? value) => value switch
    {
        null or DBNull => Option.None,
        T payload => Option.Some(payload),
        _ => Option.Some((T)Convert(value)),
    };

    private static object ToParameterValue(T payload) => payload switch
    {
        Enum enumValue => System.Convert.ChangeType(enumValue, Enum.GetUnderlyingType(enumValue.GetType()), CultureInfo.InvariantCulture),
        _ => payload!,
    };

    private static object Convert(object value) => value switch
    {
        _ when ValueType.IsEnum => Enum.ToObject(ValueType, value),
        byte[] bytes when ValueType == typeof(Guid) => new Guid(bytes),
        string text when TypeDescriptor.GetConverter(ValueType) is { } converter && converter.CanConvertFrom(typeof(string)) =>
            converter.ConvertFromInvariantString(text)!,
        _ => System.Convert.ChangeType(value, ValueType, CultureInfo.InvariantCulture),
    };
}
