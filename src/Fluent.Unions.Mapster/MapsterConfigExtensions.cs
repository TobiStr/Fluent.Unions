using System.Linq.Expressions;
using System.Reflection;
using Mapster;

namespace Fluent.Unions.Mapster;

/// <summary>
/// Registers <see cref="Option{T}"/> mapping rules with Mapster.
/// </summary>
public static class MapsterConfigExtensions
{
    private static readonly Func<CompileArgument, LambdaExpression> ConverterFactory = CreateConverter;
    private static readonly MethodInfo ToOptionMethod = typeof(OptionMapper).GetMethod(nameof(OptionMapper.ToOption))!;
    private static readonly MethodInfo MapOptionMethod = typeof(OptionMapper).GetMethod(nameof(OptionMapper.MapOption))!;
    private static readonly MethodInfo FromOptionMethod = typeof(OptionMapper).GetMethod(nameof(OptionMapper.FromOption))!;

    /// <summary>
    /// Maps <c>Option&lt;T&gt;</c> to and from <c>T?</c>, and <c>Option&lt;TSource&gt;</c> to <c>Option&lt;TDestination&gt;</c>. Calling it again on the same config changes nothing.
    /// </summary>
    public static TypeAdapterConfig AddFluentUnions(this TypeAdapterConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        lock (config)
        {
            if (config.Rules.Any(rule => rule.Settings.ConverterFactory == ConverterFactory))
                return config;

            var settings = config.When((source, destination, _) => IsOption(source) || IsOption(destination)).Settings;
            settings.ConverterFactory = ConverterFactory;
            settings.ConverterToTargetFactory = CreateConverterToTarget;
        }

        return config;
    }

    private static LambdaExpression CreateConverterToTarget(CompileArgument argument)
    {
        var converter = CreateConverter(argument);
        var destination = Expression.Parameter(argument.DestinationType, "destination");
        return Expression.Lambda(converter.Body, converter.Parameters[0], destination);
    }

    private static LambdaExpression CreateConverter(CompileArgument argument)
    {
        var source = argument.SourceType;
        var destination = argument.DestinationType;
        var method = (IsOption(source), IsOption(destination)) switch
        {
            (true, true) => MapOptionMethod.MakeGenericMethod(source.GenericTypeArguments[0], destination.GenericTypeArguments[0]),
            (true, false) => FromOptionMethod.MakeGenericMethod(source.GenericTypeArguments[0], destination),
            _ => ToOptionMethod.MakeGenericMethod(source, destination.GenericTypeArguments[0]),
        };

        var parameter = Expression.Parameter(source, "source");
        var call = Expression.Call(method, parameter, Expression.Constant(argument.Context.Config));
        return Expression.Lambda(call, parameter);
    }

    private static bool IsOption(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Option<>);
}
