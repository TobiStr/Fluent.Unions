using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Fluent.Unions.Validation;

/// <summary>
/// Named collection rules in two forms. The form on a collection returns a <see cref="Result{T}"/> of the same collection type,
/// a null collection is an error, and the argument name is captured for the message. The form on a <see cref="Result{T}"/> takes
/// an optional name, and an error or empty result passes through. <c>message</c> replaces the default message.
/// Rules that take a predicate need a typed lambda parameter, for example <c>(int x) =&gt; x &gt; 0</c>.
/// </summary>
public static class CollectionRuleExtensions
{
    /// <summary>Fails unless the collection has an item.</summary>
    public static Result<TCollection> EnsureNotEmpty<TCollection>(this TCollection value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where TCollection : IEnumerable =>
        RuleFailure.Lift(value, message, name).EnsureNotEmpty(message, name);

    /// <summary>Fails unless the collection has an item.</summary>
    public static Result<TCollection> EnsureNotEmpty<TCollection>(this Result<TCollection> result, string? message = null, string? name = null) where TCollection : IEnumerable =>
        RuleFailure.Check(result, collection => Is.NotEmptySequence(collection), "{Name} must not be empty.", message, name);

    /// <summary>Fails unless the collection has no item.</summary>
    public static Result<TCollection> EnsureEmpty<TCollection>(this TCollection value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where TCollection : IEnumerable =>
        RuleFailure.Lift(value, message, name).EnsureEmpty(message, name);

    /// <summary>Fails unless the collection has no item.</summary>
    public static Result<TCollection> EnsureEmpty<TCollection>(this Result<TCollection> result, string? message = null, string? name = null) where TCollection : IEnumerable =>
        RuleFailure.Check(result, collection => !Is.NotEmptySequence(collection), "{Name} must be empty.", message, name);

    /// <summary>Fails unless the items are all different. Items are compared with <see cref="object.Equals(object?)"/>.</summary>
    public static Result<TCollection> EnsureUnique<TCollection>(this TCollection value, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where TCollection : IEnumerable =>
        RuleFailure.Lift(value, message, name).EnsureUnique(message, name);

    /// <summary>Fails unless the items are all different. Items are compared with <see cref="object.Equals(object?)"/>.</summary>
    public static Result<TCollection> EnsureUnique<TCollection>(this Result<TCollection> result, string? message = null, string? name = null) where TCollection : IEnumerable =>
        RuleFailure.Check(result, collection => Is.UniqueSequence(collection), "{Name} must contain unique items.", message, name);

    /// <summary>Fails unless the items are all different under the comparer.</summary>
    public static Result<TCollection> EnsureUnique<TCollection, TItem>(this TCollection value, IEqualityComparer<TItem> comparer, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where TCollection : IEnumerable<TItem> =>
        RuleFailure.Lift(value, message, name).EnsureUnique(comparer, message, name);

    /// <summary>Fails unless the items are all different under the comparer.</summary>
    public static Result<TCollection> EnsureUnique<TCollection, TItem>(this Result<TCollection> result, IEqualityComparer<TItem> comparer, string? message = null, string? name = null) where TCollection : IEnumerable<TItem> =>
        RuleFailure.Check(result, Adapt<TCollection, TItem>(Is.Unique(comparer)), "{Name} must contain unique items.", message, name);

    /// <summary>Fails unless the collection has exactly the given number of items.</summary>
    public static Result<TCollection> EnsureHasCount<TCollection>(this TCollection value, int count, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where TCollection : IEnumerable =>
        RuleFailure.Lift(value, message, name).EnsureHasCount(count, message, name);

    /// <summary>Fails unless the collection has exactly the given number of items.</summary>
    public static Result<TCollection> EnsureHasCount<TCollection>(this Result<TCollection> result, int count, string? message = null, string? name = null) where TCollection : IEnumerable
    {
        var rule = Is.HasCount(count);
        return RuleFailure.Check(result, collection => rule(collection), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must contain exactly {count} items."), message, name);
    }

    /// <summary>Fails unless the collection has at least the given number of items.</summary>
    public static Result<TCollection> EnsureHasMinCount<TCollection>(this TCollection value, int count, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where TCollection : IEnumerable =>
        RuleFailure.Lift(value, message, name).EnsureHasMinCount(count, message, name);

    /// <summary>Fails unless the collection has at least the given number of items.</summary>
    public static Result<TCollection> EnsureHasMinCount<TCollection>(this Result<TCollection> result, int count, string? message = null, string? name = null) where TCollection : IEnumerable
    {
        var rule = Is.HasMinCount(count);
        return RuleFailure.Check(result, collection => rule(collection), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must contain at least {count} items."), message, name);
    }

    /// <summary>Fails unless the collection has at most the given number of items.</summary>
    public static Result<TCollection> EnsureHasMaxCount<TCollection>(this TCollection value, int count, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where TCollection : IEnumerable =>
        RuleFailure.Lift(value, message, name).EnsureHasMaxCount(count, message, name);

    /// <summary>Fails unless the collection has at most the given number of items.</summary>
    public static Result<TCollection> EnsureHasMaxCount<TCollection>(this Result<TCollection> result, int count, string? message = null, string? name = null) where TCollection : IEnumerable
    {
        var rule = Is.HasMaxCount(count);
        return RuleFailure.Check(result, collection => rule(collection), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must contain at most {count} items."), message, name);
    }

    /// <summary>Fails unless the collection contains the item.</summary>
    public static Result<TCollection> EnsureContainsItem<TCollection, TItem>(this TCollection value, TItem item, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where TCollection : IEnumerable<TItem> =>
        RuleFailure.Lift(value, message, name).EnsureContainsItem(item, message, name);

    /// <summary>Fails unless the collection contains the item.</summary>
    public static Result<TCollection> EnsureContainsItem<TCollection, TItem>(this Result<TCollection> result, TItem item, string? message = null, string? name = null) where TCollection : IEnumerable<TItem> =>
        RuleFailure.Check(result, Adapt<TCollection, TItem>(Is.ContainsItem(item)), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must contain {item}."), message, name);

    /// <summary>Fails if the collection contains the item.</summary>
    public static Result<TCollection> EnsureNotContainsItem<TCollection, TItem>(this TCollection value, TItem item, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where TCollection : IEnumerable<TItem> =>
        RuleFailure.Lift(value, message, name).EnsureNotContainsItem(item, message, name);

    /// <summary>Fails if the collection contains the item.</summary>
    public static Result<TCollection> EnsureNotContainsItem<TCollection, TItem>(this Result<TCollection> result, TItem item, string? message = null, string? name = null) where TCollection : IEnumerable<TItem> =>
        RuleFailure.Check(result, Adapt<TCollection, TItem>(Is.NotContainsItem(item)), string.Create(CultureInfo.InvariantCulture, $"{{Name}} must not contain {item}."), message, name);

    /// <summary>Fails unless every item matches. An empty collection passes.</summary>
    public static Result<TCollection> EnsureAllMatch<TCollection, TItem>(this TCollection value, Func<TItem, bool> predicate, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where TCollection : IEnumerable<TItem> =>
        RuleFailure.Lift(value, message, name).EnsureAllMatch(predicate, message, name);

    /// <summary>Fails unless every item matches. An empty collection passes.</summary>
    public static Result<TCollection> EnsureAllMatch<TCollection, TItem>(this Result<TCollection> result, Func<TItem, bool> predicate, string? message = null, string? name = null) where TCollection : IEnumerable<TItem> =>
        RuleFailure.Check(result, Adapt<TCollection, TItem>(Is.AllMatch(predicate)), "{Name} must have only items that match the condition.", message, name);

    /// <summary>Fails unless at least one item matches.</summary>
    public static Result<TCollection> EnsureAnyMatch<TCollection, TItem>(this TCollection value, Func<TItem, bool> predicate, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where TCollection : IEnumerable<TItem> =>
        RuleFailure.Lift(value, message, name).EnsureAnyMatch(predicate, message, name);

    /// <summary>Fails unless at least one item matches.</summary>
    public static Result<TCollection> EnsureAnyMatch<TCollection, TItem>(this Result<TCollection> result, Func<TItem, bool> predicate, string? message = null, string? name = null) where TCollection : IEnumerable<TItem> =>
        RuleFailure.Check(result, Adapt<TCollection, TItem>(Is.AnyMatch(predicate)), "{Name} must have at least one item that matches the condition.", message, name);

    /// <summary>Fails if any item matches. An empty collection passes.</summary>
    public static Result<TCollection> EnsureNoneMatch<TCollection, TItem>(this TCollection value, Func<TItem, bool> predicate, string? message = null, [CallerArgumentExpression(nameof(value))] string? name = null) where TCollection : IEnumerable<TItem> =>
        RuleFailure.Lift(value, message, name).EnsureNoneMatch(predicate, message, name);

    /// <summary>Fails if any item matches. An empty collection passes.</summary>
    public static Result<TCollection> EnsureNoneMatch<TCollection, TItem>(this Result<TCollection> result, Func<TItem, bool> predicate, string? message = null, string? name = null) where TCollection : IEnumerable<TItem> =>
        RuleFailure.Check(result, Adapt<TCollection, TItem>(Is.NoneMatch(predicate)), "{Name} must have no item that matches the condition.", message, name);

    private static Func<TCollection, bool> Adapt<TCollection, TItem>(Func<IEnumerable<TItem>?, bool> rule) where TCollection : IEnumerable<TItem> =>
        collection => rule(collection);
}
