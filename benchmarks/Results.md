# Mediator

## 4.5.1
| Method                                                  | Mean      | Error     | StdDev    | Gen0   | Gen1   | Allocated |
|---------------------------------------------------------|----------:|----------:|----------:|-------:|-------:|----------:|
| 'Publish with single sync subscriber'                   |  3.482 us | 0.0301 us | 0.0267 us | 1.0529 | 0.2594 |  10.49 KB |
| 'Publish with single async subscriber'                  |  5.229 us | 0.0432 us | 0.0404 us | 1.1292 | 0.2747 |   11.4 KB |
| 'Publish with multiple sync subscribers'                |  4.036 us | 0.0264 us | 0.0220 us | 1.0910 | 0.2670 |  10.91 KB |
| 'Publish with multiple async subscribers'               |  8.484 us | 0.0808 us | 0.0716 us | 1.3428 | 0.3052 |  13.65 KB |
| 'Publish with multiple sync and async subscribers'      |  5.858 us | 0.0667 us | 0.0624 us | 1.1597 | 0.2747 |  11.65 KB |
| 'PublishAsync with single sync subscriber'              |  3.396 us | 0.0177 us | 0.0166 us | 1.0376 | 0.2861 |  10.32 KB |
| 'PublishAsync with single async subscriber'             |  5.861 us | 0.0591 us | 0.0461 us | 1.1597 | 0.2441 |  11.11 KB |
| 'PublishAsync with multiple sync subscribers'           |  3.997 us | 0.0411 us | 0.0384 us | 1.0681 | 0.2670 |  10.63 KB |
| 'PublishAsync with multiple async subscribers'          | 14.188 us | 0.2578 us | 0.2411 us | 1.2817 | 0.3052 |  12.53 KB |
| 'PublishAsync with multiple sync and async subscribers' |  7.038 us | 0.1091 us | 0.1020 us | 1.1597 | 0.2441 |  11.29 KB |

## 5.0.0
| Method                                                  | Mean       | Error    | StdDev   | Gen0   | Allocated |
|-------------------------------------------------------- |-----------:|---------:|---------:|-------:|----------:|
| 'Publish with single sync subscriber'                   |   549.6 ns |  3.43 ns |  2.87 ns | 0.0849 |     896 B |
| 'Publish with single async subscriber'                  | 1,586.0 ns | 13.75 ns | 12.19 ns | 0.1297 |    1367 B |
| 'Publish with multiple sync subscribers'                | 1,295.7 ns | 20.67 ns | 19.34 ns | 0.1354 |    1424 B |
| 'Publish with multiple async subscribers'               | 4,282.9 ns | 48.49 ns | 45.36 ns | 0.2670 |    2838 B |
| 'Publish with multiple sync and async subscribers'      | 2,253.0 ns | 37.62 ns | 35.19 ns | 0.1717 |    1800 B |
| 'PublishAsync with single sync subscriber'              |   558.3 ns |  5.98 ns |  5.30 ns | 0.0849 |     896 B |
| 'PublishAsync with single async subscriber'             | 1,870.6 ns |  7.80 ns |  6.92 ns | 0.1926 |    2006 B |
| 'PublishAsync with multiple sync subscribers'           | 1,286.3 ns |  6.54 ns |  5.46 ns | 0.1354 |    1424 B |
| 'PublishAsync with multiple async subscribers'          | 5,734.0 ns | 23.99 ns | 22.44 ns | 0.4120 |    4279 B |
| 'PublishAsync with multiple sync and async subscribers' | 2,734.4 ns | 11.75 ns | 10.41 ns | 0.2708 |    2835 B |

## Comparison
| Method                                                | Mean 4.5.1 | Mean 5.0.0 | Mean Diff | Allocated 4.5.1 | Allocated 5.0.0 | Allocated Diff |
|-------------------------------------------------------|-----------:|-----------:|----------:|----------------:|----------------:|---------------:|
| Publish with single sync subscriber                   |   3.482 us |   0.550 us |     -84 % |        10.49 KB |        0.875 KB |          -92 % |
| Publish with single async subscriber                  |   5.229 us |   1.586 us |     -70 % |        11.40 KB |        1.335 KB |          -88 % |
| Publish with multiple sync subscribers                |   4.036 us |   1.296 us |     -68 % |        10.91 KB |        1.391 KB |          -87 % |
| Publish with multiple async subscribers               |   8.484 us |   4.283 us |     -50 % |        13.65 KB |        2.771 KB |          -80 % |
| Publish with multiple sync and async subscribers      |   5.858 us |   2.253 us |     -62 % |        11.65 KB |        1.758 KB |          -85 % |
| PublishAsync with single sync subscriber              |   3.396 us |   0.558 us |     -84 % |        10.32 KB |        0.875 KB |          -92 % |
| PublishAsync with single async subscriber             |   5.861 us |   1.871 us |     -68 % |        11.11 KB |        1.959 KB |          -82 % |
| PublishAsync with multiple sync subscribers           |   3.997 us |   1.286 us |     -68 % |        10.63 KB |        1.391 KB |          -87 % |
| PublishAsync with multiple async subscribers          |  14.188 us |   5.734 us |     -60 % |        12.53 KB |        4.179 KB |          -67 % |
| PublishAsync with multiple sync and async subscribers |   7.038 us |   2.734 us |     -61 % |        11.29 KB |        2.769 KB |          -75 % |

# Kafka producer

Publishing 1000 messages to Kafka to a single topic using a single producer.

## 4.5.1

| Method                        | Mean    | Error   | StdDev  | Ratio | Gen0      | Allocated | Alloc Ratio |
|------------------------------ |--------:|--------:|--------:|------:|----------:|----------:|------------:|
| 'PublishAsync inside foreach' | 15.54 s | 0.007 s | 0.006 s |  1.00 | 2000.0000 |  21.58 MB |        1.00 |

## 5.0.0

| Method                        | Mean         | Error    | StdDev   | Ratio | Gen0     | Gen1     | Allocated | Alloc Ratio |
|------------------------------ |-------------:|---------:|---------:|------:|---------:|---------:|----------:|------------:|
| 'PublishAsync inside foreach' | 15,534.45 ms | 6.023 ms | 5.634 ms | 1.000 |        - |        - |   7.24 MB |        1.00 |
| WrapAndPublishBatchAsync      |     15.61 ms | 0.207 ms | 0.193 ms | 0.001 | 421.8750 | 281.2500 |   4.32 MB |        0.60 |
