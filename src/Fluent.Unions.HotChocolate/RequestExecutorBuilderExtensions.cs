using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fluent.Unions.HotChocolate;

/// <summary>
/// Registers the Fluent.Unions support on an <see cref="IRequestExecutorBuilder"/>.
/// </summary>
public static class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Lets resolvers return <c>Result&lt;T&gt;</c> as <c>T</c> and <c>Option&lt;T&gt;</c> as a nullable <c>T</c>.
    /// </summary>
    public static IRequestExecutorBuilder AddFluentUnions(this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .TryAddTypeInterceptor<UnionTypeInterceptor>()
            .AddErrorFilter<ValidationErrorFilter>();
    }
}
