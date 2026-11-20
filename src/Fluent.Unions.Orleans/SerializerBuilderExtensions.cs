using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Orleans.Serialization.Serializers;

namespace Fluent.Unions.Orleans;

/// <summary>
/// Registers the Fluent.Unions serialization with Orleans.
/// </summary>
public static class SerializerBuilderExtensions
{
    /// <summary>
    /// Adds the converter for <see cref="Option{T}"/>, and a codec that refuses to serialize <see cref="Result"/>,
    /// <see cref="Result{T}"/> and <see cref="Result{T, TError}"/>, because an error would expose exception details.
    /// </summary>
    public static ISerializerBuilder AddUnions(this ISerializerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<IGeneralizedCodec, ResultRejectingCodec>();
        return builder.AddAssembly(typeof(OptionConverter<>).Assembly);
    }
}
