// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.KafkaEvents.Statistics
{
    [SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
    public class WindowStatistics
    {
        [JsonPropertyName("min")]
        public long Min { get; set; }

        [JsonPropertyName("max")]
        public long Max { get; set; }

        [JsonPropertyName("avg")]
        public long Avg { get; set; }

        [JsonPropertyName("sum")]
        public long Sum { get; set; }

        [JsonPropertyName("stddev")]
        public long StdDev { get; set; }

        [JsonPropertyName("p50")]
        public long P50 { get; set; }

        [JsonPropertyName("p75")]
        public long P75 { get; set; }

        [JsonPropertyName("p90")]
        public long P90 { get; set; }

        [JsonPropertyName("p95")]
        public long P95 { get; set; }

        [JsonPropertyName("p99")]
        public long P99 { get; set; }

        [JsonPropertyName("p99_99")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Named after the JSON field")]
        [SuppressMessage("", "CA1707", Justification = "Named after the JSON field")]
        public long P99_99 { get; set; }

        [JsonPropertyName("outofrange")]
        public long OutOfRange { get; set; }

        [JsonPropertyName("hdrsize")]
        public long HdrSize { get; set; }

        [JsonPropertyName("cnt")]
        public long Cnt { get; set; }
    }
}
