```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 11.0.100
  [Host] : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3  
LaunchCount=1  WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev    | Gen0   | Allocated |
|------------------- |----------:|----------:|----------:|-------:|----------:|
| CreateValuePayload |  7.260 ns | 37.033 ns | 2.0299 ns | 0.0029 |      24 B |
| ChainReference     | 29.788 ns | 11.704 ns | 0.6415 ns | 0.0067 |      56 B |
| SelectValue        | 25.579 ns | 53.875 ns | 2.9531 ns | 0.0095 |      80 B |
| Ensure             | 49.176 ns | 60.263 ns | 3.3032 ns | 0.0181 |     152 B |
| Match              | 17.597 ns | 14.657 ns | 0.8034 ns | 0.0029 |      24 B |
