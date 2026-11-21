using AutoMapper;
using AutoMapper.Internal;

namespace Fluent.Unions.AutoMapper;

/// <summary>
/// Registers <see cref="Option{T}"/> mapping with AutoMapper.
/// </summary>
public static class AutoMapperConfigExtensions
{
    /// <summary>
    /// Maps <c>Option&lt;T&gt;</c> to and from <c>T?</c>, and <c>Option&lt;TSource&gt;</c> to <c>Option&lt;TDestination&gt;</c>. Calling it again on the same configuration changes nothing.
    /// </summary>
    public static IMapperConfigurationExpression AddFluentUnions(this IMapperConfigurationExpression configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var mappers = configuration.Internal().Mappers;

        lock (mappers)
        {
            if (!mappers.Any(mapper => mapper is OptionMapper))
                mappers.Insert(0, new OptionMapper());
        }

        return configuration;
    }
}
