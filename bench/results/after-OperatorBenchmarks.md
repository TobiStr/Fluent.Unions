```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 11.0.100
  [Host] : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3  
LaunchCount=1  WarmupCount=3  

```
| Method             | Mean       | Error      | StdDev    | Median     | Gen0   | Allocated |
|------------------- |-----------:|-----------:|----------:|-----------:|-------:|----------:|
| CreateValuePayload |  0.3197 ns |  7.1733 ns | 0.3932 ns |  0.1513 ns |      - |         - |
| SelectReference    |  3.0227 ns | 15.4725 ns | 0.8481 ns |  2.7922 ns |      - |         - |
| SelectValue        |  4.8506 ns | 35.0080 ns | 1.9189 ns |  4.1002 ns |      - |         - |
| ChainReference     | 13.1954 ns | 15.6075 ns | 0.8555 ns | 12.9570 ns |      - |         - |
| ChainCapturing     | 24.7013 ns | 95.3373 ns | 5.2258 ns | 23.7037 ns | 0.0048 |      40 B |
| ChainOnError       |  9.7438 ns | 21.7585 ns | 1.1927 ns |  9.2629 ns |      - |         - |
| Ensure             |  4.6288 ns |  2.7704 ns | 0.1519 ns |  4.6407 ns |      - |         - |
| Do                 |  4.2820 ns |  1.0835 ns | 0.0594 ns |  4.2580 ns |      - |         - |
| Match              |  2.9510 ns | 10.6569 ns | 0.5841 ns |  3.1053 ns |      - |         - |
| ChainOnTask        | 43.0929 ns | 23.1354 ns | 1.2681 ns | 43.3885 ns | 0.0105 |      88 B |
| Pipeline           | 38.3462 ns | 16.6262 ns | 0.9113 ns | 38.6069 ns | 0.0038 |      32 B |
