using NSwag.Generation;

namespace Fluent.Unions.NSwag;

/// <summary>
/// Registers the Fluent.Unions schema support on <see cref="OpenApiDocumentGeneratorSettings"/>.
/// </summary>
public static class OpenApiDocumentGeneratorSettingsExtensions
{
    /// <summary>
    /// Describes <c>Option&lt;T&gt;</c> as a nullable <c>T</c> in the generated document.
    /// </summary>
    public static OpenApiDocumentGeneratorSettings AddFluentUnions(this OpenApiDocumentGeneratorSettings settings)
    {
        settings.SchemaSettings.SchemaProcessors.Add(new OptionSchemaProcessor());
        return settings;
    }
}
