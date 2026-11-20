namespace Fluent.Unions;

/// <summary>
/// Operators for sequences of results.
/// </summary>
public static class ResultCollectionExtensions
{
    /// <summary>
    /// Collects all values into one result. The first error wins.
    /// </summary>
    public static Result<IReadOnlyList<T>> Combine<T>(this IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return Capture.Run(() =>
        {
            var values = new List<T>();

            foreach (var result in results)
            {
                switch (result.Case)
                {
                    case Exception error:
                        return error;
                    case T value:
                        values.Add(value);
                        break;
                }
            }

            return Result<IReadOnlyList<T>>.Of(values);
        });
    }

    /// <summary>
    /// Collects all values into one result, or every error: a <see cref="ValidationErrors"/> when all of them are
    /// validation failures, otherwise an <see cref="AggregateException"/>.
    /// </summary>
    public static Result<IReadOnlyList<T>> CombineAll<T>(this IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return Capture.Run(() =>
        {
            var (values, errors) = results.Partition();

            return errors.Count == 0
                ? Result<IReadOnlyList<T>>.Of(values)
                : ValidationErrors.Combine(errors);
        });
    }

    /// <summary>
    /// Splits the results into values and errors in one pass.
    /// </summary>
    public static (IReadOnlyList<T> Values, IReadOnlyList<Exception> Errors) Partition<T>(this IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var values = new List<T>();
        var errors = new List<Exception>();

        foreach (var result in results)
        {
            switch (result.Case)
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

        return results.Select(result => result.Case).Where(value => value is not Exception).OfType<T>();
    }

    /// <summary>
    /// Returns the errors of all failed results.
    /// </summary>
    public static IEnumerable<Exception> Errors<T>(this IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.Select(result => result.Case).OfType<Exception>();
    }
}
