using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fluent.Unions.Swashbuckle;

/// <summary>
/// Registers the Fluent.Unions schema support on <see cref="SwaggerGenOptions"/>.
/// </summary>
public static class SwaggerGenOptionsExtensions
{
    /// <summary>
    /// Describes <c>Option&lt;T&gt;</c> as a nullable <c>T</c> in the generated document.
    /// </summary>
    public static SwaggerGenOptions AddFluentUnions(this SwaggerGenOptions options)
    {
        options.SchemaFilter<OptionSchemaFilter>();
        return options;
    }
}
