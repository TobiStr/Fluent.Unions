using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fluent.Unions.NewtonsoftJson;

/// <summary>
/// Serializes your own unions with the case name, so they can be read back:
/// <c>{"$type":"Circle","$value":{"Radius":2}}</c>. Apply it with <c>[JsonConverter(typeof(UnionJsonConverter))]</c>.
/// An empty union is written as <c>null</c>.
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
        return unionType.IsValueType && typeof(IUnion).IsAssignableFrom(unionType);
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

        while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
        {
            var property = (string)reader.Value!;
            reader.Read();

            if (property == TypeProperty)
            {
                var name = reader.TokenType == JsonToken.String ? (string)reader.Value! : null;

                if (name is null)
                    throw new JsonSerializationException($"A {unionType.Name} needs a '{TypeProperty}' property.");

                if (!cases.ByName.TryGetValue(name, out constructor))
                    throw new JsonSerializationException($"'{name}' is not a case of {unionType.Name}.");
            }
            else if (property == ValueProperty)
            {
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

        if (buffered is not null)
            value = buffered.ToObject(constructor.GetParameters()[0].ParameterType, serializer);

        return constructor.Invoke([value]);
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not IUnion { Value: { } payload } union)
        {
            writer.WriteNull();
            return;
        }

        var cases = GetCases(union.GetType());
        var caseType = cases.NamesByType.Keys.FirstOrDefault(t => t == payload.GetType())
            ?? cases.NamesByType.Keys.First(t => t.IsInstanceOfType(payload));

        writer.WriteStartObject();
        writer.WritePropertyName(TypeProperty);
        writer.WriteValue(cases.NamesByType[caseType]);
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
        CasesByUnion.GetOrAdd(unionType, static type =>
        {
            var constructors = type
                .GetConstructors()
                .Where(c => c.GetParameters().Length == 1)
                .ToList();
            var duplicate = constructors
                .GroupBy(c => c.GetParameters()[0].ParameterType.Name)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicate is not null)
                throw new InvalidOperationException($"{type} has several cases named '{duplicate.Key}'.");

            return new Cases(
                constructors.ToDictionary(c => c.GetParameters()[0].ParameterType.Name),
                constructors.ToDictionary(c => c.GetParameters()[0].ParameterType, c => c.GetParameters()[0].ParameterType.Name));
        });

    private sealed record Cases(Dictionary<string, ConstructorInfo> ByName, Dictionary<Type, string> NamesByType);
}
