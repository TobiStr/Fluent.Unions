using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fluent.Unions.Swashbuckle;

/// <summary>
/// Describes <c>Option&lt;T&gt;</c> as a nullable <c>T</c>, matching how it is written as JSON.
/// </summary>
public sealed class OptionSchemaFilter : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var optionType = context.Type;
        if (!optionType.IsGenericType || optionType.GetGenericTypeDefinition() != typeof(Option<>)
            || schema is not OpenApiSchema target)
        {
            return;
        }

        var valueType = optionType.GenericTypeArguments[0];
        var inner = context.SchemaGenerator.GenerateSchema(valueType, context.SchemaRepository);

        target.Properties = null;
        target.Required = null;
        target.AdditionalProperties = null;
        target.AdditionalPropertiesAllowed = true;

        if (inner is OpenApiSchema { Type: { } type } plain && !type.HasFlag(JsonSchemaType.Null))
        {
            CopyInto(target, plain);
            target.Type = type | JsonSchemaType.Null;
            return;
        }

        target.Type = null;
        target.OneOf = [new OpenApiSchema { Type = JsonSchemaType.Null }, inner];
    }

    private static void CopyInto(OpenApiSchema target, OpenApiSchema source)
    {
        target.Format = source.Format;
        target.Pattern = source.Pattern;
        target.Minimum = source.Minimum;
        target.Maximum = source.Maximum;
        target.MinLength = source.MinLength;
        target.MaxLength = source.MaxLength;
        target.Enum = WithNull(source.Enum);
        target.Description ??= source.Description;
        target.Title ??= source.Title;
        target.Default ??= source.Default;
        target.Examples ??= source.Examples;
        target.Deprecated |= source.Deprecated;
        target.Items = source.Items;
        target.Properties = source.Properties;
        target.Required = source.Required;
        target.AdditionalProperties = source.AdditionalProperties;
        target.AdditionalPropertiesAllowed = source.AdditionalPropertiesAllowed;
    }

    private static List<JsonNode>? WithNull(IList<JsonNode>? values)
    {
        if (values is null)
        {
            return null;
        }

        var withNull = new List<JsonNode>(values);
        if (!withNull.Contains(null!))
        {
            withNull.Add(null!);
        }

        return withNull;
    }
}
