using AutoMapper;
using AutoMapper.Internal;

namespace Fluent.Unions.AutoMapper;

/// <summary>
/// Registers <see cref="Option{T}"/> mapping with AutoMapper.
/// </summary>
public static class AutoMapperConfigExtensions
{
    /// <summary>
    /// Maps <c>Option&lt;T&gt;</c> to and from <c>T?</c>, and <c>Option&lt;TSource&gt;</c> to <c>Option&lt;TDestination&gt;</c>.
    /// </summary>
    public static IMapperConfigurationExpression AddFluentUnions(this IMapperConfigurationExpression configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        configuration.Internal().Mappers.Insert(0, new OptionMapper());
        return configuration;
    }
}
