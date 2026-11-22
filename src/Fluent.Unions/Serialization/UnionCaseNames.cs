using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Fluent.Unions.Serialization;

/// <summary>
/// Gives a case of a union a stable name in JSON, independent of the name of its type. Put it on the union, once per case:
/// <c>[UnionCaseName(typeof(Circle), "circle", Aliases = ["Circle"])]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class UnionCaseNameAttribute(Type caseType, string name) : Attribute
{
    /// <summary>
    /// The case type.
    /// </summary>
    public Type CaseType { get; } = caseType;

    /// <summary>
    /// The name that is written and read.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Further names that are read, such as the name before a rename.
    /// </summary>
    public string[] Aliases { get; init; } = [];
}

/// <summary>
/// The JSON names of the cases of a union: the name of the case type, or the one from <see cref="UnionCaseNameAttribute"/>.
/// </summary>
public sealed class UnionCaseNames
{
    private static readonly ConcurrentDictionary<Type, UnionCaseNames> ByUnion = new();

    private readonly Dictionary<Type, string> namesByType;
    private readonly Dictionary<string, Type> typesByName;

    private UnionCaseNames(Dictionary<Type, string> namesByType, Dictionary<string, Type> typesByName)
    {
        this.namesByType = namesByType;
        this.typesByName = typesByName;
    }

    /// <summary>
    /// The case types of the union.
    /// </summary>
    public IReadOnlyCollection<Type> CaseTypes => namesByType.Keys;

    /// <summary>
    /// Gets the names of the cases of <paramref name="unionType"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Two cases share a name, or a <see cref="UnionCaseNameAttribute"/> is invalid.</exception>
    public static UnionCaseNames For([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type unionType)
    {
        ArgumentNullException.ThrowIfNull(unionType);

        return ByUnion.TryGetValue(unionType, out var names) ? names : ByUnion.GetOrAdd(unionType, Create(unionType));
    }

    /// <summary>
    /// The name that is written for <paramref name="caseType"/>.
    /// </summary>
    public string NameOf(Type caseType) => namesByType[caseType];

    /// <summary>
    /// Gets the case type for a name or an alias.
    /// </summary>
    public bool TryGetCaseType(string name, [NotNullWhen(true)] out Type? caseType) => typesByName.TryGetValue(name, out caseType);

    private static UnionCaseNames Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type unionType)
    {
        var caseTypes = unionType.GetConstructors()
            .Select(constructor => constructor.GetParameters())
            .Where(parameters => parameters.Length == 1)
            .Select(parameters => parameters[0].ParameterType)
            .ToList();
        var attributes = unionType.GetCustomAttributes<UnionCaseNameAttribute>(inherit: false).ToList();

        foreach (var attribute in attributes)
        {
            if (!caseTypes.Contains(attribute.CaseType))
                throw new InvalidOperationException($"[UnionCaseName] on {unionType} names {attribute.CaseType}, which is not a case of it.");
            if (string.IsNullOrWhiteSpace(attribute.Name) || attribute.Aliases.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException($"[UnionCaseName] on {unionType} for {attribute.CaseType} has an empty name or alias.");
        }

        if (attributes.GroupBy(attribute => attribute.CaseType).FirstOrDefault(group => group.Count() > 1) is { } repeated)
            throw new InvalidOperationException($"{unionType} has several [UnionCaseName] attributes for {repeated.Key}.");

        var namesByType = new Dictionary<Type, string>();
        var typesByName = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var caseType in caseTypes)
        {
            var attribute = attributes.FirstOrDefault(candidate => candidate.CaseType == caseType);
            var name = attribute?.Name ?? caseType.Name;
            namesByType[caseType] = name;

            IEnumerable<string> readNames = attribute is null ? [name] : attribute.Aliases.Prepend(name);
            foreach (var readName in readNames)
            {
                if (typesByName.TryGetValue(readName, out var other) && other != caseType)
                {
                    throw new InvalidOperationException(
                        $"{unionType} has several cases named '{readName}': {other} and {caseType}. Give them distinct names with [UnionCaseName].");
                }

                typesByName[readName] = caseType;
            }
        }

        return new UnionCaseNames(namesByType, typesByName);
    }
}
