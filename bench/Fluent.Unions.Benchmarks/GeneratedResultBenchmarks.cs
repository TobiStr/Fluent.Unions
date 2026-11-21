using BenchmarkDotNet.Attributes;
using Fluent.Unions.Abstractions;

namespace Fluent.Unions.Benchmarks;

/// <summary>
/// Measures operator calls on a result type declared with <c>[FluentResult]</c>.
/// </summary>
[MemoryDiagnoser]
public class GeneratedResultBenchmarks
{
    private readonly GeneratedResult<string> text = "value";
    private readonly GeneratedResult<int> number = 42;

    [Benchmark]
    public GeneratedResult<int> CreateValuePayload() => 42;

    [Benchmark]
    public GeneratedResult<string> ChainReference() => text.Chain(static s => GeneratedResult<string>.Ok(s));

    [Benchmark]
    public GeneratedResult<int> SelectValue() => number.Select(static n => n + 1);

    [Benchmark]
    public GeneratedResult<string> Ensure() => text.Ensure(static s => s.Length > 0, "empty");

    [Benchmark]
    public int Match() => text.Match(static s => s.Length, static _ => -1);
}

/// <summary>
/// The error base of the generated benchmark domain.
/// </summary>
public class GeneratedDomainException(string message, Exception? inner = null)
    : Exception(message, inner), IFromException<GeneratedDomainException>
{
    /// <inheritdoc />
    public static GeneratedDomainException FromException(Exception exception) =>
        exception as GeneratedDomainException ?? new GeneratedDomainException(exception.Message, exception);
}

/// <summary>
/// A result type with a domain error, declared with the generator.
/// </summary>
[FluentResult<GeneratedDomainException>]
public readonly partial struct GeneratedResult<T>;
