namespace ResultType.Extensions;

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
