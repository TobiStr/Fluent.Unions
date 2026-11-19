using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Fluent.Unions.OpenApi;

/// <summary>
/// Describes <c>Option&lt;T&gt;</c> as a nullable <c>T</c>, matching how it is written as JSON.
/// </summary>
public sealed class OptionSchemaTransformer : IOpenApiSchemaTransformer
{
    private static readonly AsyncLocal<ImmutableHashSet<Type>?> Resolving = new();

    /// <inheritdoc />
    public async Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var optionType = context.JsonTypeInfo.Type;
        if (!optionType.IsGenericType || optionType.GetGenericTypeDefinition() != typeof(Option<>))
        {
            return;
        }

        var valueType = optionType.GenericTypeArguments[0];
        var resolving = Resolving.Value ?? ImmutableHashSet<Type>.Empty;
        if (resolving.Contains(valueType))
        {
            return;
        }

        IOpenApiSchema inner;
        Resolving.Value = resolving.Add(valueType);
        try
        {
            inner = await context.GetOrCreateSchemaAsync(valueType, null, cancellationToken);
        }
        finally
        {
            Resolving.Value = resolving;
        }

        var nullSchema = new OpenApiSchema { Type = JsonSchemaType.Null };

        if (inner is OpenApiSchema { Type: { } type } plain && !type.HasFlag(JsonSchemaType.Null))
        {
            CopyInto(schema, plain);
            schema.Type = type | JsonSchemaType.Null;
            return;
        }

        schema.OneOf = [nullSchema, inner];
    }

    private static void CopyInto(OpenApiSchema target, OpenApiSchema source)
    {
        target.Type = source.Type;
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
