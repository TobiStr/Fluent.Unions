using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Fluent.Unions.NewtonsoftJson;

/// <summary>
/// Registers the Fluent.Unions converters with Newtonsoft.Json.
/// </summary>
public static class JsonSerializerSettingsExtensions
{
    /// <summary>
    /// Adds <see cref="OptionJsonConverter"/> and <see cref="ResultJsonConverter"/>. Calling it again changes nothing.
    /// </summary>
    [RequiresUnreferencedCode("Creates converters through reflection. Not supported in trimmed or native AOT apps; see the AOT and trimming page of the docs.")]
    [RequiresDynamicCode("Creates converters through reflection. Not supported in trimmed or native AOT apps; see the AOT and trimming page of the docs.")]
    public static JsonSerializerSettings AddFluentUnions(this JsonSerializerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.Converters.OfType<OptionJsonConverter>().Any())
            settings.Converters.Add(new OptionJsonConverter());

        if (!settings.Converters.OfType<ResultJsonConverter>().Any())
            settings.Converters.Add(new ResultJsonConverter());

        return settings;
    }
}
