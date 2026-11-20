namespace Fluent.Unions;

/// <summary>
/// Rx-style operators that run side effects and return the same result, unless the side effect throws.
/// </summary>
public static class ResultSideEffectExtensions
{
    /// <summary>
    /// Runs <paramref name="onSuccess"/> with the value.
    /// </summary>
    public static Result<T> Do<T>(this Result<T> result, Action<T> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result.Case switch
        {
            Exception => result,
            T value => Capture.Run(() =>
            {
                onSuccess(value);
                return result;
            }),
            _ => result
        };
    }

    /// <summary>
    /// Runs <paramref name="onSuccess"/> with the value, or <paramref name="onError"/> with the error.
    /// </summary>
    public static Result<T> Do<T>(this Result<T> result, Action<T> onSuccess, Action<Exception> onError)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);

        return result.Case switch
        {
            Exception error => Capture.Run(() =>
            {
                onError(error);
                return result;
            }),
            T value => Capture.Run(() =>
            {
                onSuccess(value);
                return result;
            }),
            _ => result
        };
    }

    /// <summary>
    /// Runs <paramref name="onSuccess"/> after a successful <see cref="Result"/>.
    /// </summary>
    public static Result Do(this Result result, Action onSuccess)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result.Case is Ok
            ? Capture.Run(() =>
            {
                onSuccess();
                return result;
            })
            : result;
    }

    /// <summary>
    /// Runs <paramref name="onSuccess"/> after success, or <paramref name="onError"/> with the error.
    /// </summary>
    public static Result Do(this Result result, Action onSuccess, Action<Exception> onError)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);

        return result.Case switch
        {
            Exception error => Capture.Run(() =>
            {
                onError(error);
                return result;
            }),
            Ok => Capture.Run(() =>
            {
                onSuccess();
                return result;
            }),
            _ => result
        };
    }

    /// <summary>
    /// Runs <paramref name="onError"/> with the error.
    /// </summary>
    public static Result<T> OnErrorDo<T>(this Result<T> result, Action<Exception> onError)
    {
        ArgumentNullException.ThrowIfNull(onError);

        return result.Case is Exception error
            ? Capture.Run(() =>
            {
                onError(error);
                return result;
            })
            : result;
    }

    /// <summary>
    /// Runs <paramref name="onError"/> with the error.
    /// </summary>
    public static Result OnErrorDo(this Result result, Action<Exception> onError)
    {
        ArgumentNullException.ThrowIfNull(onError);

        return result.Case is Exception error
            ? Capture.Run(() =>
            {
                onError(error);
                return result;
            })
            : result;
    }

    /// <summary>
    /// Captures the value for later steps of the chain and returns the same result.
    /// <paramref name="value"/> is filled only when the result holds a value.
    /// </summary>
    public static Result<T> Out<T>(this Result<T> result, out Captured<T> value)
    {
        value = new Captured<T>();
        value.SetFrom(result);

        return result;
    }

    /// <summary>
    /// Runs <paramref name="action"/> for every result, value or error.
    /// </summary>
    public static Result<T> Finally<T>(this Result<T> result, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return Capture.Run(() =>
        {
            action();
            return result;
        });
    }

    /// <summary>
    /// Runs <paramref name="action"/> for every result, success or error.
    /// </summary>
    public static Result Finally(this Result result, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return Capture.Run(() =>
        {
            action();
            return result;
        });
    }
}
