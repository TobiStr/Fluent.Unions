using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluent.Unions.Serialization;

/// <summary>
/// Serializes your own unions with the case name, so they can be read back:
/// <c>{"$type":"Circle","$value":{"Radius":2}}</c>. Apply it with <c>[JsonConverter(typeof(UnionJsonConverter))]</c>.
/// An empty union is written as <c>null</c>. Result types and <see cref="Option{T}"/> keep their own converters.
/// </summary>
public sealed class UnionJsonConverter : JsonConverterFactory
{
    /// <summary>
    /// The property that holds the case name.
    /// </summary>
    public const string TypeProperty = "$type";

    /// <summary>
    /// The property that holds the case value.
    /// </summary>
    public const string ValueProperty = "$value";

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsValueType
        && typeof(IUnion).IsAssignableFrom(typeToConvert)
        && !Abstractions.ResultTypes.IsResult(typeToConvert)
        && !(typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Option<>));

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(typeToConvert))!;

    private sealed class Converter<TUnion> : JsonConverter<TUnion> where TUnion : struct, IUnion
    {
        private readonly Dictionary<string, ConstructorInfo> casesByName;
        private readonly Dictionary<Type, string> namesByType;

        public Converter()
        {
            var constructors = typeof(TUnion)
                .GetConstructors()
                .Where(c => c.GetParameters().Length == 1)
                .ToList();
            var duplicate = constructors
                .GroupBy(c => c.GetParameters()[0].ParameterType.Name)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicate is not null)
                throw new InvalidOperationException($"{typeof(TUnion)} has several cases named '{duplicate.Key}'.");

            casesByName = constructors.ToDictionary(c => c.GetParameters()[0].ParameterType.Name);
            namesByType = constructors.ToDictionary(c => c.GetParameters()[0].ParameterType, c => c.GetParameters()[0].ParameterType.Name);
        }

        public override bool HandleNull => true;

        public override TUnion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return default;

            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            if (!root.TryGetProperty(TypeProperty, out var typeName) || typeName.GetString() is not { } name)
                throw new JsonException($"A {typeof(TUnion).Name} needs a '{TypeProperty}' property.");

            if (!casesByName.TryGetValue(name, out var constructor))
                throw new JsonException($"'{name}' is not a case of {typeof(TUnion).Name}.");

            var caseType = constructor.GetParameters()[0].ParameterType;
            var value = root.TryGetProperty(ValueProperty, out var element)
                ? element.Deserialize(caseType, options)
                : null;

            return (TUnion)constructor.Invoke([value]);
        }

        public override void Write(Utf8JsonWriter writer, TUnion value, JsonSerializerOptions options)
        {
            if (value.Value is not { } payload)
            {
                writer.WriteNullValue();
                return;
            }

            var caseType = namesByType.Keys.FirstOrDefault(t => t == payload.GetType())
                ?? namesByType.Keys.First(t => t.IsInstanceOfType(payload));

            writer.WriteStartObject();
            writer.WriteString(TypeProperty, namesByType[caseType]);
            writer.WritePropertyName(ValueProperty);
            JsonSerializer.Serialize(writer, payload, caseType, options);
            writer.WriteEndObject();
        }
    }
}
