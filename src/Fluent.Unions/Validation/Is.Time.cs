namespace Fluent.Unions.Validation;

public static partial class Is
{
    /// <summary>True if the duration is greater than zero.</summary>
    public static bool Positive(TimeSpan value) => value > TimeSpan.Zero;

    /// <summary>True if the duration is less than zero.</summary>
    public static bool Negative(TimeSpan value) => value < TimeSpan.Zero;

    /// <summary>True if the duration is zero or less.</summary>
    public static bool NonPositive(TimeSpan value) => value <= TimeSpan.Zero;

    /// <summary>True if the duration is zero or greater.</summary>
    public static bool NonNegative(TimeSpan value) => value >= TimeSpan.Zero;

    /// <summary>True if the duration is zero.</summary>
    public static bool Zero(TimeSpan value) => value == TimeSpan.Zero;

    /// <summary>True if the duration is not zero.</summary>
    public static bool NonZero(TimeSpan value) => value != TimeSpan.Zero;

    /// <summary>Creates a predicate for instants before the current time of the provider, read at check time.</summary>
    public static Func<DateTimeOffset, bool> InPast(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return value => value < timeProvider.GetUtcNow();
    }

    /// <summary>Creates a predicate for instants before <paramref name="now"/>.</summary>
    public static Func<DateTimeOffset, bool> InPast(DateTimeOffset now) => value => value < now;

    /// <summary>Creates a predicate for dates before <paramref name="now"/>.</summary>
    public static Func<DateTime, bool> InPast(DateTime now) => value => value < now;

    /// <summary>Creates a predicate for instants up to the current time of the provider, read at check time.</summary>
    public static Func<DateTimeOffset, bool> InPastOrPresent(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return value => value <= timeProvider.GetUtcNow();
    }

    /// <summary>Creates a predicate for instants up to and including <paramref name="now"/>.</summary>
    public static Func<DateTimeOffset, bool> InPastOrPresent(DateTimeOffset now) => value => value <= now;

    /// <summary>Creates a predicate for dates up to and including <paramref name="now"/>.</summary>
    public static Func<DateTime, bool> InPastOrPresent(DateTime now) => value => value <= now;

    /// <summary>Creates a predicate for instants after the current time of the provider, read at check time.</summary>
    public static Func<DateTimeOffset, bool> InFuture(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return value => value > timeProvider.GetUtcNow();
    }

    /// <summary>Creates a predicate for instants after <paramref name="now"/>.</summary>
    public static Func<DateTimeOffset, bool> InFuture(DateTimeOffset now) => value => value > now;

    /// <summary>Creates a predicate for dates after <paramref name="now"/>.</summary>
    public static Func<DateTime, bool> InFuture(DateTime now) => value => value > now;

    /// <summary>Creates a predicate for instants from the current time of the provider on, read at check time.</summary>
    public static Func<DateTimeOffset, bool> InFutureOrPresent(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return value => value >= timeProvider.GetUtcNow();
    }

    /// <summary>Creates a predicate for instants from <paramref name="now"/> on, including it.</summary>
    public static Func<DateTimeOffset, bool> InFutureOrPresent(DateTimeOffset now) => value => value >= now;

    /// <summary>Creates a predicate for dates from <paramref name="now"/> on, including it.</summary>
    public static Func<DateTime, bool> InFutureOrPresent(DateTime now) => value => value >= now;
}
