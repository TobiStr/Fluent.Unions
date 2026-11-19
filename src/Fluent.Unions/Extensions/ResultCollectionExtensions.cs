namespace Fluent.Unions.Extensions;

/// <summary>
/// Operators for sequences of results.
/// </summary>
public static class ResultCollectionExtensions
{
    /// <summary>
    /// Collects all values into one result. The first error wins, and empty results are skipped.
    /// </summary>
    public static Result<IReadOnlyList<T>> Combine<T>(this IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return Capture.Run(() =>
        {
            var values = new List<T>();

            foreach (var result in results)
            {
                switch (result.Value)
                {
                    case Exception error:
                        return error;
                    case T value:
                        values.Add(value);
                        break;
                }
            }

            return new Result<IReadOnlyList<T>>(values);
        });
    }

    /// <summary>
    /// Collects all values into one result, or every error in an <see cref="AggregateException"/>. Empty results are skipped.
    /// </summary>
    public static Result<IReadOnlyList<T>> CombineAll<T>(this IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return Capture.Run(() =>
        {
            var (values, errors) = results.Partition();

            return errors.Count == 0
                ? new Result<IReadOnlyList<T>>(values)
                : new AggregateException(errors).Flatten();
        });
    }

    /// <summary>
    /// Splits the results into values and errors in one pass. Empty results are skipped.
    /// </summary>
    public static (IReadOnlyList<T> Values, IReadOnlyList<Exception> Errors) Partition<T>(this IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var values = new List<T>();
        var errors = new List<Exception>();

        foreach (var result in results)
        {
            switch (result.Value)
            {
                case Exception error:
                    errors.Add(error);
                    break;
                case T value:
                    values.Add(value);
                    break;
            }
        }

        return (values, errors);
    }

    /// <summary>
    /// Returns the values of all successful results.
    /// </summary>
    public static IEnumerable<T> Values<T>(this IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.Select(result => result.Value).Where(value => value is not Exception).OfType<T>();
    }

    /// <summary>
    /// Returns the errors of all failed results.
    /// </summary>
    public static IEnumerable<Exception> Errors<T>(this IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.Select(result => result.Value).OfType<Exception>();
    }
}
