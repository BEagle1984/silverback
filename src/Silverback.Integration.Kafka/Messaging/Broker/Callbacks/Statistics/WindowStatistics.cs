// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.Broker.Callbacks.Statistics;

[SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
public class WindowStatistics
{
    [JsonPropertyName("min")]
    public long Min { get; init; }

    [JsonPropertyName("max")]
    public long Max { get; init; }

    [JsonPropertyName("avg")]
    public long Avg { get; init; }

    [JsonPropertyName("sum")]
    public long Sum { get; init; }

    [JsonPropertyName("stddev")]
    public long StdDev { get; init; }

    [JsonPropertyName("p50")]
    public long P50 { get; init; }

    [JsonPropertyName("p75")]
    public long P75 { get; init; }

    [JsonPropertyName("p90")]
    public long P90 { get; init; }

    [JsonPropertyName("p95")]
    public long P95 { get; init; }

    [JsonPropertyName("p99")]
    public long P99 { get; init; }

    [JsonPropertyName("p99_99")]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Named after the JSON field")]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Named after the JSON field")]
    public long P99_99 { get; init; }

    [JsonPropertyName("outofrange")]
    public long OutOfRange { get; init; }

    [JsonPropertyName("hdrsize")]
    public long HdrSize { get; init; }

    [JsonPropertyName("cnt")]
    public long Cnt { get; init; }
}
