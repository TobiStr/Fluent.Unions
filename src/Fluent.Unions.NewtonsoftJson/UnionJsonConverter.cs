using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fluent.Unions.Abstractions;
using Fluent.Unions.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fluent.Unions.NewtonsoftJson;

/// <summary>
/// Serializes your own unions with the case name, so they can be read back. Give a case a stable name with <see cref="UnionCaseNameAttribute"/>:
/// <c>{"$type":"Circle","$value":{"Radius":2}}</c>. Apply it with <c>[JsonConverter(typeof(UnionJsonConverter))]</c>.
/// An empty union is written as <c>null</c>. Results and <see cref="Option{T}"/> are never handled here, so they keep their own converters.
/// </summary>
public sealed class UnionJsonConverter : JsonConverter
{
    /// <summary>
    /// The property that holds the case name.
    /// </summary>
    public const string TypeProperty = "$type";

    /// <summary>
    /// The property that holds the case value.
    /// </summary>
    public const string ValueProperty = "$value";

    private static readonly ConcurrentDictionary<Type, Cases> CasesByUnion = new();

    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
    {
        var unionType = Nullable.GetUnderlyingType(objectType) ?? objectType;
        return unionType.IsValueType
            && typeof(IUnion).IsAssignableFrom(unionType)
            && !ResultTypes.IsResult(unionType)
            && !(unionType.IsGenericType && unionType.GetGenericTypeDefinition() == typeof(Option<>));
    }

    /// <inheritdoc />
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var unionType = Nullable.GetUnderlyingType(objectType) ?? objectType;

        if (reader.TokenType == JsonToken.Null)
            return unionType == objectType ? Activator.CreateInstance(unionType) : null;

        if (reader.TokenType != JsonToken.StartObject)
            throw new JsonSerializationException($"A {unionType.Name} must be a JSON object or null.");

        var cases = GetCases(unionType);
        ConstructorInfo? constructor = null;
        object? value = null;
        JToken? buffered = null;
        var hasValue = false;

        while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
        {
            var property = (string)reader.Value!;
            reader.Read();

            if (property == TypeProperty)
            {
                var name = reader.TokenType == JsonToken.String ? (string)reader.Value! : null;

                if (name is null)
                    throw new JsonSerializationException($"A {unionType.Name} needs a '{TypeProperty}' property.");

                if (!cases.Names.TryGetCaseType(name, out var caseType))
                    throw new JsonSerializationException($"'{name}' is not a case of {unionType.Name}.");

                constructor = cases.Constructors[caseType];
            }
            else if (property == ValueProperty)
            {
                hasValue = true;
                if (constructor is null)
                    buffered = ReadBuffered(reader);
                else
                    value = serializer.Deserialize(reader, constructor.GetParameters()[0].ParameterType);
            }
            else
                reader.Skip();
        }

        if (constructor is null)
            throw new JsonSerializationException($"A {unionType.Name} needs a '{TypeProperty}' property.");

        if (!hasValue)
            throw new JsonSerializationException($"A {unionType.Name} needs a '{ValueProperty}' property.");

        if (buffered is not null)
            value = buffered.ToObject(constructor.GetParameters()[0].ParameterType, serializer);

        var union = constructor.Invoke([value]);

        return ((IUnion)union).Value is null
            ? throw new JsonSerializationException($"The '{ValueProperty}' of case '{cases.Names.NameOf(constructor.GetParameters()[0].ParameterType)}' is null. An empty {unionType.Name} is written as null.")
            : union;
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not IUnion { Value: { } payload } union)
        {
            writer.WriteNull();
            return;
        }

        var names = GetCases(union.GetType()).Names;
        var caseType = names.CaseTypes.FirstOrDefault(t => t == payload.GetType())
            ?? names.CaseTypes.First(t => t.IsInstanceOfType(payload));

        writer.WriteStartObject();
        writer.WritePropertyName(TypeProperty);
        writer.WriteValue(names.NameOf(caseType));
        writer.WritePropertyName(ValueProperty);
        serializer.Serialize(writer, payload, caseType);
        writer.WriteEndObject();
    }

    private static JToken ReadBuffered(JsonReader reader)
    {
        var dateParseHandling = reader.DateParseHandling;
        reader.DateParseHandling = DateParseHandling.None;

        try
        {
            return JToken.ReadFrom(reader);
        }
        finally
        {
            reader.DateParseHandling = dateParseHandling;
        }
    }

    private static Cases GetCases(Type unionType) =>
        CasesByUnion.GetOrAdd(unionType, static type => new Cases(
            UnionCaseNames.For(type),
            type.GetConstructors()
                .Where(c => c.GetParameters().Length == 1)
                .ToDictionary(c => c.GetParameters()[0].ParameterType)));

    private sealed record Cases(UnionCaseNames Names, Dictionary<Type, ConstructorInfo> Constructors);
}
