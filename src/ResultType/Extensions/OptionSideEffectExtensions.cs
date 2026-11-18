namespace ResultType.Extensions;

/// <summary>
/// Rx-style operators that run side effects and return the same option.
/// </summary>
public static class OptionSideEffectExtensions
{
    /// <summary>
    /// Runs <paramref name="onNext"/> with the value.
    /// </summary>
    public static Option<T> Do<T>(this Option<T> option, Action<T> onNext)
    {
        ArgumentNullException.ThrowIfNull(onNext);

        if (option.TryGetValue(out var value))
            onNext(value);

        return option.Normalize();
    }

    /// <summary>
    /// Runs <paramref name="onNext"/> with the value, or <paramref name="onNone"/> when the option holds none.
    /// </summary>
    public static Option<T> Do<T>(this Option<T> option, Action<T> onNext, Action onNone)
    {
        ArgumentNullException.ThrowIfNull(onNext);
        ArgumentNullException.ThrowIfNull(onNone);

        if (option.TryGetValue(out var value))
            onNext(value);
        else
            onNone();

        return option.Normalize();
    }

    /// <summary>
    /// Runs <paramref name="onNone"/> when the option holds no value.
    /// </summary>
    public static Option<T> OnNoneDo<T>(this Option<T> option, Action onNone)
    {
        ArgumentNullException.ThrowIfNull(onNone);

        if (!option.HasValue)
            onNone();

        return option.Normalize();
    }

    /// <summary>
    /// Runs <paramref name="action"/> for every option, with or without a value.
    /// </summary>
    public static Option<T> Finally<T>(this Option<T> option, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        action();
        return option.Normalize();
    }
}
