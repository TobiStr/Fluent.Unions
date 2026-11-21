namespace Fluent.Unions;

internal static class ResultErrors
{
    internal static InvalidOperationException Uninitialized(Type resultType) => new(
        $"{Name(resultType)} holds neither a value nor an error. It is default, or it was created from null; " +
        "use Option<T> for a value that may be missing.");

    internal static string Name(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;

        var tick = type.Name.IndexOf('`');
        var name = tick < 0 ? type.Name : type.Name[..tick];

        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(Name))}>";
    }
}
