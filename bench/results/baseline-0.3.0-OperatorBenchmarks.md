```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 11.0.100
  [Host] : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3  
LaunchCount=1  WarmupCount=3  

```
| Method             | Mean       | Error      | StdDev    | Gen0   | Allocated |
|------------------- |-----------:|-----------:|----------:|-------:|----------:|
| CreateValuePayload |   6.188 ns |  25.406 ns |  1.393 ns | 0.0029 |      24 B |
| SelectReference    |  24.098 ns |  69.291 ns |  3.798 ns | 0.0067 |      56 B |
| SelectValue        |  31.657 ns | 225.635 ns | 12.368 ns | 0.0067 |      56 B |
| ChainReference     |  31.585 ns | 118.473 ns |  6.494 ns | 0.0038 |      32 B |
| ChainCapturing     | 247.095 ns | 100.065 ns |  5.485 ns | 0.0153 |     128 B |
| ChainOnError       |  61.822 ns |  55.573 ns |  3.046 ns | 0.0038 |      32 B |
| Ensure             | 180.555 ns | 244.251 ns | 13.388 ns | 0.0153 |     128 B |
| Do                 |  87.292 ns |  83.051 ns |  4.552 ns | 0.0048 |      40 B |
| Match              |   6.105 ns |  38.625 ns |  2.117 ns |      - |         - |
| ChainOnTask        | 284.640 ns | 586.019 ns | 32.122 ns | 0.0401 |     336 B |
| Pipeline           | 203.644 ns | 417.817 ns | 22.902 ns | 0.0315 |     264 B |
