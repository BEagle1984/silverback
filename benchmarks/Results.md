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
| Method                                                  | Mean 4.5.1 | Mean 5.0.0 | Mean Diff | Allocated 4.5.1 | Allocated 5.0.0 | Allocated Diff |
|---------------------------------------------------------|-----------:|-----------:|----------:|----------------:|----------------:|---------------:|
| Publish with single sync subscriber                     |   3.482 us |  0.5496 us |   -84.22% |        10.49 KB |        0.875 KB |        -91.66% |
| Publish with single async subscriber                    |   5.229 us |  1.5860 us |   -69.67% |        11.40 KB |        1.335 KB |        -88.29% |
| Publish with multiple sync subscribers                  |   4.036 us |  1.2957 us |   -67.90% |        10.91 KB |        1.391 KB |        -87.25% |
| Publish with multiple async subscribers                 |   8.484 us |  4.2829 us |   -49.52% |        13.65 KB |        2.771 KB |        -79.70% |
| Publish with multiple sync and async subscribers        |   5.858 us |  2.2530 us |   -61.54% |        11.65 KB |        1.758 KB |        -84.91% |
| PublishAsync with single sync subscriber                |   3.396 us |  0.5583 us |   -83.55% |        10.32 KB |        0.875 KB |        -91.52% |
| PublishAsync with single async subscriber               |   5.861 us |  1.8706 us |   -68.09% |        11.11 KB |        1.959 KB |        -82.37% |
| PublishAsync with multiple sync subscribers             |   3.997 us |  1.2863 us |   -67.81% |        10.63 KB |        1.391 KB |        -86.91% |
| PublishAsync with multiple async subscribers            |  14.188 us |  5.7340 us |   -59.58% |        12.53 KB |        4.178 KB |        -66.65% |
| PublishAsync with multiple sync and async subscribers   |   7.038 us |  2.7344 us |   -61.15% |        11.29 KB |        2.770 KB |        -75.46% |
