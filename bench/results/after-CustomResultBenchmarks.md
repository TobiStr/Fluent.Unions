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
| CreateValuePayload |  3.415 ns | 11.199 ns | 0.6139 ns | 0.0029 |      24 B |
| ChainReference     | 13.221 ns |  5.940 ns | 0.3256 ns | 0.0067 |      56 B |
| SelectValue        | 16.658 ns |  1.713 ns | 0.0939 ns | 0.0095 |      80 B |
| Ensure             | 27.420 ns |  9.634 ns | 0.5281 ns | 0.0181 |     152 B |
| Match              |  9.498 ns | 46.107 ns | 2.5273 ns | 0.0029 |      24 B |
