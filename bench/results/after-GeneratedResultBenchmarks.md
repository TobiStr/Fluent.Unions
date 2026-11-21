```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 11.0.100
  [Host] : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3  
LaunchCount=1  WarmupCount=3  

```
| Method             | Mean      | Error      | StdDev    | Median    | Allocated |
|------------------- |----------:|-----------:|----------:|----------:|----------:|
| CreateValuePayload | 0.7483 ns |  5.7052 ns | 0.3127 ns | 0.5979 ns |         - |
| ChainReference     | 7.5124 ns |  3.3649 ns | 0.1844 ns | 7.4292 ns |         - |
| SelectValue        | 4.4757 ns | 21.7370 ns | 1.1915 ns | 4.8065 ns |         - |
| Ensure             | 4.2305 ns |  1.6770 ns | 0.0919 ns | 4.2279 ns |         - |
| Match              | 1.8835 ns | 11.1582 ns | 0.6116 ns | 2.0791 ns |         - |
