using BenchmarkDotNet.Attributes;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Benchmarks;

/// <summary>
/// Measures operator calls on a result type with its own error type.
/// </summary>
[MemoryDiagnoser]
public class CustomResultBenchmarks
{
    private readonly DomainResult<string> text = "value";
    private readonly DomainResult<int> number = 42;

    [Benchmark]
    public DomainResult<int> CreateValuePayload() => 42;

    [Benchmark]
    public DomainResult<string> ChainReference() => text.Chain(static s => (DomainResult<string>)s);

    [Benchmark]
    public DomainResult<int> SelectValue() =>
        number.Select<DomainResult<int>, int, BenchmarkDomainException, DomainResult<int>, int>(static n => n + 1);

    [Benchmark]
    public DomainResult<string> Ensure() => text.Ensure(static s => s.Length > 0, "empty");

    [Benchmark]
    public int Match() => text.Match(static s => s.Length, static _ => -1);
}

/// <summary>
/// The error base of the benchmark domain.
/// </summary>
public class BenchmarkDomainException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// A hand-written result type with a domain error, as documented for 0.3.0.
/// </summary>
public union DomainResult<T>(T, BenchmarkDomainException) : IResult<DomainResult<T>, T, BenchmarkDomainException>
{
    /// <inheritdoc />
    public static DomainResult<T> FromValue(T value) => value;

    /// <inheritdoc />
    public static DomainResult<T> FromError(BenchmarkDomainException error) => error;

    /// <inheritdoc />
    public static BenchmarkDomainException FromException(Exception exception) =>
        exception as BenchmarkDomainException ?? new BenchmarkDomainException(exception.Message, exception);
}
