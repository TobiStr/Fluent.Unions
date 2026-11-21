using BenchmarkDotNet.Attributes;

namespace Fluent.Unions.Benchmarks;

/// <summary>
/// Measures the cost of single operator calls on the default result type.
/// </summary>
[MemoryDiagnoser]
public class OperatorBenchmarks
{
    private readonly Result<string> text = "value";
    private readonly Result<string> failed = new InvalidOperationException("failed");
    private readonly Result<int> number = 42;
    private readonly Task<Result<string>> completed = Task.FromResult<Result<string>>("value");
    private int offset = 1;

    [Benchmark]
    public Result<int> CreateValuePayload() => offset;

    [Benchmark]
    public Result<int> SelectReference() => text.Select(static s => s.Length);

    [Benchmark]
    public Result<int> SelectValue() => number.Select(static n => n + 1);

    [Benchmark]
    public Result<string> ChainReference() => text.Chain(static s => Result<string>.Ok(s));

    [Benchmark]
    public Result<string> ChainCapturing() => text.Chain(s => Result<string>.Ok(s + offset));

    [Benchmark]
    public Result<string> ChainOnError() => failed.Chain(static s => Result<string>.Ok(s));

    [Benchmark]
    public Result<string> Ensure() => text.Ensure(static s => s.Length > 0, "empty");

    [Benchmark]
    public Result<string> Do() => text.Do(static _ => { });

    [Benchmark]
    public int Match() => text.Match(static s => s.Length, static _ => -1);

    [Benchmark]
    public Task<Result<string>> ChainOnTask() => completed.Chain(static s => Result<string>.Ok(s));

    [Benchmark]
    public Result<string> Pipeline() => text
        .Select(static s => s.Trim())
        .Ensure(static s => s.Length > 0, "empty")
        .Chain(static s => Result<string>.Ok(s.ToUpperInvariant()))
        .Do(static _ => { });
}
