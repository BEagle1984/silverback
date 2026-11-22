---
uid: performance
description: "BenchmarkDotNet results comparing Silverback 5.10.0 with 5.9.0 and 5.0.0 with 4.6.2: execution time and memory allocations of the in-memory message bus and the Kafka producer and consumer."
---

# Performance

Silverback tries to improve the performance with every release and a huge effort is put into optimizing the code and the integration with the underlying libraries. The development of v5.0.0 especially focused on performance improvements in several areas and the results speak for themselves. There surely is still room for further optimizations, but the current state is already quite satisfying.

The benchmarks were built with .NET 10 and run on a Windows 11 workstation (AMD Ryzen 9 9950X3D, 64 GB DDR5). They use [BenchmarkDotNet](https://benchmarkdotnet.org/), and the source code is available on [GitHub](https://github.com/BEagle1984/silverback/tree/main/tests/extended).

## v5.10.0 vs. v5.9.0

v5.10.0 cuts the allocations made by the message bus for each published message by up to 80 % and trims the Kafka producer and consumer pipelines, for example in the header mapping, in the propagation of the trace baggage and by handing the serialized message to the producer without copying it.

Each version was run twice, alternating them, and the tables show the average of the two runs. The allocations vary by a few bytes at most, while the times vary by up to about 9 % between the runs: the time savings of the message bus are well above that, while for Kafka the allocations are the reliable signal and the time differences are within the noise. These benchmarks run in process, in a different session than the v5.0.0 vs. v4.6.2 comparison below, so only the numbers within the same section are comparable.

### Message Bus

| Method                                                | Mean 5.9.0 | Mean 5.10.0 | Mean Diff | Allocated 5.9.0 | Allocated 5.10.0 | Allocated Diff |
|-------------------------------------------------------|-----------:|------------:|----------:|----------------:|-----------------:|---------------:|
| Publish with single sync subscriber                   |   241.8 ns |    110.9 ns |     -54 % |         1.08 KB |          0.22 KB |          -80 % |
| Publish with single async subscriber                  |   669.3 ns |    533.8 ns |     -20 % |         1.30 KB |          0.45 KB |          -66 % |
| Publish with multiple sync subscribers                |   245.9 ns |    112.9 ns |     -54 % |         1.08 KB |          0.22 KB |          -80 % |
| Publish with multiple async subscribers               |   672.7 ns |    542.8 ns |     -19 % |         1.30 KB |          0.45 KB |          -66 % |
| Publish with multiple sync and async subscribers      |   881.1 ns |    748.8 ns |     -15 % |         1.77 KB |          0.96 KB |          -46 % |
| PublishAsync with single sync subscriber              |   251.2 ns |    124.8 ns |     -50 % |         1.08 KB |          0.22 KB |          -80 % |
| PublishAsync with single async subscriber             | 1,017.9 ns |    751.6 ns |     -26 % |         2.24 KB |          1.33 KB |          -41 % |
| PublishAsync with multiple sync subscribers           |   258.2 ns |    128.3 ns |     -50 % |         1.08 KB |          0.22 KB |          -80 % |
| PublishAsync with multiple async subscribers          | 1,021.9 ns |    753.9 ns |     -26 % |         2.24 KB |          1.32 KB |          -41 % |
| PublishAsync with multiple sync and async subscribers | 1,382.3 ns |  1,160.4 ns |     -16 % |         3.09 KB |          2.23 KB |          -28 % |

### Kafka Producer and Consumer

Producing and consuming a JSON message of about 200 bytes with the mocked Kafka broker of `Silverback.Integration.Kafka.Testing`, which replaces the Confluent client and the network. In the variants with the trace baggage, the messages are produced under an `Activity` carrying three baggage items, which travel in the message headers.

| Method                     | Mean 5.9.0 | Mean 5.10.0 | Mean Diff | Allocated 5.9.0 | Allocated 5.10.0 | Allocated Diff |
|----------------------------|-----------:|------------:|----------:|----------------:|-----------------:|---------------:|
| Produce                    |    2.71 us |     2.59 us |      -5 % |         4.83 KB |          4.68 KB |           -3 % |
| Produce with trace baggage |    3.32 us |     3.01 us |     -10 % |         5.62 KB |          5.16 KB |           -8 % |
| Consume                    |    4.58 us |     4.43 us |      -3 % |         9.67 KB |          8.96 KB |           -7 % |
| Consume with trace baggage |    4.92 us |     4.64 us |      -6 % |        10.96 KB |          9.77 KB |          -11 % |

The mocked broker itself does a little more work since v5.10.0: it copies each produced message, as the Confluent client does, and allocates a bit more for each consumed message. The Kafka results therefore slightly understate the savings. In particular, they don't show the copy of the serialized message that v5.10.0 saves when producing to a real broker: one allocation the size of the message for each produced message.

## v5.0.0 vs. v4.6.2

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

| Method                      | Mean    | Error   | StdDev  | Ratio | Allocated |
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

\* `WrapAndPublishBatchAsync` did not exist in v4.6.2 so it's compared against `PublishAsync` inside a `foreach`, being it the only straightforward possibility in v4.6.2.
