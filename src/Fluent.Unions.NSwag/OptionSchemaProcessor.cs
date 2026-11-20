using Namotion.Reflection;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Fluent.Unions.NSwag;

/// <summary>
/// Describes <c>Option&lt;T&gt;</c> as a nullable <c>T</c>, matching how it is written as JSON.
/// </summary>
public sealed class OptionSchemaProcessor : ISchemaProcessor
{
    /// <inheritdoc />
    /// <exception cref="NotSupportedException">The document is a Swagger 2 document.</exception>
    public void Process(SchemaProcessorContext context)
    {
        var optionType = context.ContextualType.Type;
        if (!optionType.IsGenericType || optionType.GetGenericTypeDefinition() != typeof(Option<>))
        {
            return;
        }

        if (context.Settings.SchemaType == SchemaType.Swagger2)
        {
            throw new NotSupportedException(
                "Option<T> cannot be described in a Swagger 2 document. Generate an OpenAPI 3 document, for example with AddOpenApiDocument.");
        }

        var inner = context.Generator.GenerateWithReferenceAndNullability<JsonSchema>(
            optionType.GenericTypeArguments[0].ToContextualType(),
            true,
            context.Resolver);

        CopyInto(context.Schema, inner);
    }

    private static void CopyInto(JsonSchema target, JsonSchema source)
    {
        target.Properties.Clear();
        target.RequiredProperties.Clear();
        target.Type = source.Type;
        target.IsNullableRaw = source.IsNullableRaw;
        target.Format = source.Format;
        target.Pattern = source.Pattern;
        target.Minimum = source.Minimum;
        target.Maximum = source.Maximum;
        target.MinLength = source.MinLength;
        target.MaxLength = source.MaxLength;
        target.Description = source.Description;
        target.Title = source.Title;
        target.Default = source.Default;
        target.Example = source.Example;
        target.IsDeprecated = source.IsDeprecated;
        target.Item = source.Item;
        target.AllowAdditionalProperties = source.AllowAdditionalProperties;
        target.AdditionalPropertiesSchema = source.AdditionalPropertiesSchema;

        foreach (var value in source.Enumeration)
        {
            target.Enumeration.Add(value);
        }

        if (source.OneOf.Count == 0)
        {
            return;
        }

        target.IsNullableRaw = null;
        target.OneOf.Add(new JsonSchema { IsNullableRaw = true, Enumeration = { null } });
        foreach (var schema in source.OneOf)
        {
            target.OneOf.Add(schema);
        }
    }
}
