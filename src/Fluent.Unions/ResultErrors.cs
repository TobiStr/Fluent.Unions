namespace Fluent.Unions;

internal static class ResultErrors
{
    internal static InvalidOperationException Uninitialized(Type resultType) => new(
        $"{Name(resultType)} holds neither a value nor an error. It is default, or it was created from null; " +
        "use Option<T> for a value that may be missing.");

    internal static string Name(Type type) => type.IsGenericType
        ? $"{type.Name[..type.Name.IndexOf('`')]}<{string.Join(", ", type.GetGenericArguments().Select(Name))}>"
        : type.Name;
}
