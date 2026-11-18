namespace ResultType;

/// <summary>
/// Marks a successful <see cref="Result"/>.
/// </summary>
public readonly record struct Ok;

/// <summary>
/// Used to determine, whether a method executes successfully or not. Has implicit cast implemented:
/// Return <see cref="Exception"/> (or inherited type) to return a Result with Success set to 'false'.
/// </summary>
public union Result(Ok, Exception)
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public bool Success => Value is Ok;

    /// <summary>
    /// Gets the message of the Exception, if available.
    /// </summary>
    public string Message => (Value as Exception)?.Message ?? "";

    /// <summary>
    /// Gets the error payload of the failed result.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public Exception GetError() => Value as Exception
        ?? throw new InvalidOperationException("Error property for this Result not set.");

    /// <summary>
    /// Returns a positive Result.
    /// </summary>
    public static Result Ok => new Ok();

    /// <summary>
    /// Returns a negative Result with an Exception as payload.
    /// </summary>
    public static Result Error(Exception error) => error;
}

/// <summary>
/// Used to determine, whether a method executes successfully or not. Has implicit casts implemented:
/// Return <typeparamref name="T"/> to return a successful Result with this payload.
/// Return <see cref="Exception"/> (or inherited type) to return a Result with Success set to 'false'.
/// </summary>
/// <typeparam name="T">The type of the payload.</typeparam>
public union Result<T>(T, Exception)
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public bool Success => Value is T;

    /// <summary>
    /// Gets the message of the Exception, if available.
    /// </summary>
    public string Message => (Value as Exception)?.Message ?? "";

    /// <summary>
    /// Gets the underlying payload of the successful result.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public T GetOk() => Value switch
    {
        T payload => payload,
        Exception => throw new InvalidOperationException($"Operation for Result<{typeof(T)}> was not successful."),
        _ => throw new InvalidOperationException($"Payload for Result<{typeof(T)}> was not set.")
    };

    /// <summary>
    /// Gets the error payload of the failed result.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public Exception GetError() => Value as Exception
        ?? throw new InvalidOperationException($"Error property for Result<{typeof(T)}> not set.");

    /// <summary>
    /// Returns a positive Result with an object as payload.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public static Result<T> Ok(T payload) =>
        new(payload ?? throw new ArgumentNullException(nameof(payload)));

    /// <summary>
    /// Returns a negative Result with an Exception as payload.
    /// </summary>
    public static Result<T> Error(Exception error) => error;

    /// <summary>
    /// Implicit cast from Result&lt;T&gt; to <see cref="Result"/>, keeping Success and the error.
    /// </summary>
    public static implicit operator Result(Result<T> result) => result.Value switch
    {
        Exception error => error,
        T => Result.Ok,
        _ => default
    };

    public override string ToString() => Value?.ToString() ?? "";
}
