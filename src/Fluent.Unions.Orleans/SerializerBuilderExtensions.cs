using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Orleans.Serialization.Configuration;
using Orleans.Serialization.Serializers;

namespace Fluent.Unions.Orleans;

/// <summary>
/// Registers the Fluent.Unions serialization with Orleans.
/// </summary>
public static class SerializerBuilderExtensions
{
    /// <summary>
    /// Adds the converter for <see cref="Option{T}"/>, and a codec and a copier that refuse to serialize or copy <see cref="Result"/>,
    /// <see cref="Result{T}"/> and your own result types, because an error would expose exception details. Calling it again changes nothing.
    /// </summary>
    public static ISerializerBuilder AddFluentUnions(this ISerializerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Services.Any(service => service.ServiceType == typeof(Registered)))
            return builder;

        builder.Services.AddSingleton<Registered>();
        builder.Services.AddSingleton<IGeneralizedCodec, ResultRejectingCodec>();
        builder.Services.Configure<TypeManifestOptions>(options =>
        {
            options.Copiers.Add(typeof(ResultRejectingCopier));
            options.Copiers.Add(typeof(ResultRejectingCopier<>));
        });
        return builder.AddAssembly(typeof(OptionConverter<>).Assembly);
    }

    private sealed class Registered;
}
