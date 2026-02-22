---
uid: performance
---

# Performance

Silverback tries to improve the performance with every release and a huge effort is put into optimizing the code and the integration with the underlying libraries. The development of v5.0.0 especially focused on performance improvements in several areas and the results speak for themselves. There surely is still room for further optimizations, but the current state is already quite satisfying.

## Benchmark Results

The benchmarks were built with .NET 10 and run on a Windows 11 workstation (AMD Ryzen 9 9950X3D, 64 GB DDR5). They use [BenchmarkDotNet](https://benchmarkdotnet.org/), and the source code is available on [GitHub](https://github.com/BEagle1984/silverback/tree/master/benchmarks).

### Message Bus

#### v4.6.2
| Method                                                | Mean     | Error     | StdDev    | Gen0   | Gen1   | Allocated |
|------------------------------------------------------ |---------:|----------:|----------:|-------:|-------:|----------:|
| Publish with single sync subscriber                   | 1.726 us | 0.0296 us | 0.0247 us | 0.2136 | 0.2060 |   10.5 KB |
| Publish with single async subscriber                  | 2.572 us | 0.0230 us | 0.0204 us | 0.2365 | 0.2289 |  11.42 KB |
| Publish with multiple sync subscribers                | 1.868 us | 0.0156 us | 0.0138 us | 0.2213 | 0.2136 |  10.75 KB |
| Publish with multiple async subscribers               | 4.317 us | 0.0463 us | 0.0410 us | 0.2747 | 0.2594 |  13.45 KB |
| Publish with multiple sync and async subscribers      | 2.750 us | 0.0494 us | 0.0588 us | 0.2441 | 0.2365 |  11.73 KB |
| PublishAsync with single sync subscriber              | 1.664 us | 0.0119 us | 0.0093 us | 0.2136 | 0.2060 |  10.31 KB |
| PublishAsync with single async subscriber             | 2.508 us | 0.0312 us | 0.0276 us | 0.2365 | 0.2289 |  11.05 KB |
| PublishAsync with multiple sync subscribers           | 1.851 us | 0.0145 us | 0.0300 us | 0.2136 | 0.1984 |  10.66 KB |
| PublishAsync with multiple async subscribers          | 4.406 us | 0.0880 us | 0.1736 us | 0.2441 | 0.1831 |  12.51 KB |
| PublishAsync with multiple sync and async subscribers | 2.799 us | 0.0532 us | 0.0497 us | 0.2441 | 0.2289 |  11.36 KB |

#### v5.0.0
| Method                                                | Mean       | Error    | StdDev   | Gen0   | Allocated |
|------------------------------------------------------ |-----------:|---------:|---------:|-------:|----------:|
| Publish with single sync subscriber                   |   195.5 ns |  2.55 ns |  2.13 ns | 0.0219 |   1.08 KB |
| Publish with single async subscriber                  |   843.5 ns | 15.90 ns | 14.87 ns | 0.0305 |   1.54 KB |
| Publish with multiple sync subscribers                |   198.3 ns |  1.63 ns |  1.44 ns | 0.0219 |   1.08 KB |
| Publish with multiple async subscribers               |   853.3 ns |  3.77 ns |  2.94 ns | 0.0305 |   1.54 KB |
| Publish with multiple sync and async subscribers      | 1,037.7 ns | 12.75 ns | 11.30 ns | 0.0381 |      2 KB |
| PublishAsync with single sync subscriber              |   205.0 ns |  1.09 ns |  0.97 ns | 0.0219 |   1.08 KB |
| PublishAsync with single async subscriber             | 1,025.1 ns | 20.23 ns | 21.64 ns | 0.0458 |   2.24 KB |
| PublishAsync with multiple sync subscribers           |   204.4 ns |  1.83 ns |  1.71 ns | 0.0219 |   1.08 KB |
| PublishAsync with multiple async subscribers          | 1,027.4 ns | 15.70 ns | 15.42 ns | 0.0458 |   2.24 KB |
| PublishAsync with multiple sync and async subscribers | 1,370.5 ns | 11.19 ns |  9.92 ns | 0.0610 |   3.09 KB |



#### Comparison v5.0.0 vs. v4.6.2
| Method                                                | Mean 4.6.2 | Mean 5.0.0 | Mean Diff | Allocated 4.6.2 | Allocated 5.0.0 | Allocated Diff |
|-------------------------------------------------------|-----------:|-----------:|----------:|----------------:|----------------:|---------------:|
| Publish with single sync subscriber                   |   1.726 us |   0.196 us |     -89 % |        10.50 KB |         1.08 KB |          -90 % |
| Publish with single async subscriber                  |   2.572 us |   0.844 us |     -67 % |        11.42 KB |         1.54 KB |          -87 % |
| Publish with multiple sync subscribers                |   1.868 us |   0.198 us |     -89 % |        10.75 KB |         1.08 KB |          -90 % |
| Publish with multiple async subscribers               |   4.317 us |   0.853 us |     -80 % |        13.45 KB |         1.54 KB |          -89 % |
| Publish with multiple sync and async subscribers      |   2.750 us |   1.038 us |     -62 % |        11.73 KB |         2.00 KB |          -83 % |
| PublishAsync with single sync subscriber              |   1.664 us |   0.205 us |     -88 % |        10.31 KB |         1.08 KB |          -90 % |
| PublishAsync with single async subscriber             |   2.508 us |   1.025 us |     -59 % |        11.05 KB |         2.24 KB |          -80 % |
| PublishAsync with multiple sync subscribers           |   1.851 us |   0.204 us |     -89 % |        10.66 KB |         1.08 KB |          -90 % |
| PublishAsync with multiple async subscribers          |   4.406 us |   1.027 us |     -77 % |        12.51 KB |         2.24 KB |          -82 % |
| PublishAsync with multiple sync and async subscribers |   2.799 us |   1.371 us |     -51 % |        11.36 KB |         3.09 KB |          -73 % |

### Kafka Producer

Publishing 1000 messages to Kafka (running in docker on the local machine) to a single topic using a single producer.

#### v4.6.2

| ethod                       | Mean    | Error   | StdDev  | Ratio | Allocated |
|-----------------------------|--------:|--------:|--------:|------:|----------:|
| PublishAsync inside foreach | 15.68 s | 0.025 s | 0.024 s |  1.00 |  20.17 MB |

#### v5.0.0

| Method                      | Mean         | Error     | StdDev    | Ratio | Gen0    | Gen1    | Allocated | Alloc Ratio |
|-----------------------------|-------------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
| PublishAsync inside foreach | 15,603.73 ms | 15.956 ms | 12.457 ms | 1.000 |       - |       - |   6.31 MB |        1.00 |
| WrapAndPublishBatchAsync    |     15.59 ms |  0.139 ms |  0.123 ms | 0.001 | 62.5000 | 31.2500 |   3.43 MB |        0.54 |

#### Comparison v5.0.0 vs. v4.6.2

| Method                                                | Mean 4.6.2 | Mean 5.0.0 | Mean Diff | Allocated 4.6.2 | Allocated 5.0.0 | Allocated Diff |
|-------------------------------------------------------|-----------:|-----------:|----------:|----------------:|----------------:|---------------:|
| PublishAsync inside foreach                           |    15.68 s |    15.60 s |         - |        20.17 MB |         6.31 MB |          -69 % |
| WrapAndPublishBatchAsync                              |   *15.68 s |     0.02 s |   -99.9 % |        20.17 MB |         3.43 MB |          -83 % |

\* `WrapAndPublishBatchAsync` did not exist in v4.6.2 so it's compared against `PublishAsync` inside a `foreach`, being it the only straightforward possibility in v4.5.1.
