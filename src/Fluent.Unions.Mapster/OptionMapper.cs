using Mapster;

namespace Fluent.Unions.Mapster;

internal static class OptionMapper
{
    public static Option<TDestination> ToOption<TSource, TDestination>(TSource source, TypeAdapterConfig config) =>
        source is null ? Option.None : Wrap(config.GetMapFunction<TSource, TDestination>()(source));

    public static Option<TDestination> MapOption<TSource, TDestination>(Option<TSource> source, TypeAdapterConfig config) =>
        source.TryGetValue(out var value) ? Wrap(config.GetMapFunction<TSource, TDestination>()(value)) : Option.None;

    public static TDestination? FromOption<TSource, TDestination>(Option<TSource> source, TypeAdapterConfig config) =>
        source.TryGetValue(out var value) ? config.GetMapFunction<TSource, TDestination>()(value) : default;

    private static Option<T> Wrap<T>(T value) => value is null ? Option.None : new Option<T>(value);
}
