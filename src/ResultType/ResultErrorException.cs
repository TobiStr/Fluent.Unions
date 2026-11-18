namespace ResultType;

/// <summary>
/// Thrown by <c>Check()</c> when a result fails with an error that is not an exception.
/// </summary>
public sealed class ResultErrorException(object error) : Exception($"The result failed with {error}.")
{
    /// <summary>
    /// The error the result held.
    /// </summary>
    public object Error { get; } = error;
}
