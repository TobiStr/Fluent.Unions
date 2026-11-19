using Microsoft.AspNetCore.OpenApi;

namespace Fluent.Unions.OpenApi;

/// <summary>
/// Registers the Fluent.Unions schema support on <see cref="OpenApiOptions"/>.
/// </summary>
public static class OpenApiOptionsExtensions
{
    /// <summary>
    /// Describes <c>Option&lt;T&gt;</c> as a nullable <c>T</c> in the generated document.
    /// </summary>
    public static OpenApiOptions AddFluentUnions(this OpenApiOptions options) =>
        options.AddSchemaTransformer(new OptionSchemaTransformer());
}
