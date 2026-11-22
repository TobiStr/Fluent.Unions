using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluent.Unions.Serialization;

/// <summary>
/// Serializes your own unions with the case name, so they can be read back. Give a case a stable name with <see cref="UnionCaseNameAttribute"/>:
/// <c>{"$type":"Circle","$value":{"Radius":2}}</c>. Apply it with <c>[JsonConverter(typeof(UnionJsonConverter))]</c>.
/// An empty union is written as <c>null</c>. Result types and <see cref="Option{T}"/> keep their own converters.
/// In a trimmed or native AOT app, apply <see cref="UnionJsonConverter{TUnion}"/> instead.
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
    [UnconditionalSuppressMessage("Trimming", "IL2071", Justification = "Trimmed apps apply UnionJsonConverter<TUnion>, which keeps the constructors of the union.")]
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new NotSupportedException(
                $"{typeToConvert.Name} needs [JsonConverter(typeof(UnionJsonConverter<{typeToConvert.Name}>))] in a native AOT app.");
        }

        try
        {
            return (JsonConverter)Activator.CreateInstance(typeof(UnionJsonConverter<>).MakeGenericType(typeToConvert))!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Throw(inner);
            throw;
        }
    }
}

/// <summary>
/// Serializes one union with the case name, like <see cref="UnionJsonConverter"/>, without creating converters at runtime.
/// Apply it with <c>[JsonConverter(typeof(UnionJsonConverter&lt;Shape&gt;))]</c> in a trimmed or native AOT app.
/// </summary>
/// <typeparam name="TUnion">The union type.</typeparam>
public sealed class UnionJsonConverter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TUnion> : JsonConverter<TUnion>
    where TUnion : struct, IUnion
{
    private readonly UnionCaseNames names = UnionCaseNames.For(typeof(TUnion));
    private readonly Dictionary<Type, ConstructorInfo> constructors = typeof(TUnion)
        .GetConstructors()
        .Where(c => c.GetParameters().Length == 1)
        .ToDictionary(c => c.GetParameters()[0].ParameterType);

    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override TUnion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return default;

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException($"A {typeof(TUnion).Name} must be a JSON object or null.");

        if (!root.TryGetProperty(UnionJsonConverter.TypeProperty, out var typeName) || typeName.ValueKind != JsonValueKind.String || typeName.GetString() is not { } name)
            throw new JsonException($"A {typeof(TUnion).Name} needs a '{UnionJsonConverter.TypeProperty}' property.");

        if (!names.TryGetCaseType(name, out var caseType))
            throw new JsonException($"'{name}' is not a case of {typeof(TUnion).Name}.");

        if (!root.TryGetProperty(UnionJsonConverter.ValueProperty, out var element))
            throw new JsonException($"A {typeof(TUnion).Name} needs a '{UnionJsonConverter.ValueProperty}' property.");

        var union = (TUnion)constructors[caseType].Invoke([element.Deserialize(options.GetTypeInfo(caseType))]);

        return union.Value is null
            ? throw new JsonException($"The '{UnionJsonConverter.ValueProperty}' of case '{name}' is null. An empty {typeof(TUnion).Name} is written as null.")
            : union;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TUnion value, JsonSerializerOptions options)
    {
        if (value.Value is not { } payload)
        {
            writer.WriteNullValue();
            return;
        }

        var caseType = names.CaseTypes.FirstOrDefault(t => t == payload.GetType())
            ?? names.CaseTypes.First(t => t.IsInstanceOfType(payload));

        writer.WriteStartObject();
        writer.WriteString(UnionJsonConverter.TypeProperty, names.NameOf(caseType));
        writer.WritePropertyName(UnionJsonConverter.ValueProperty);
        JsonSerializer.Serialize(writer, payload, options.GetTypeInfo(caseType));
        writer.WriteEndObject();
    }
}
