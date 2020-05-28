// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

#pragma warning disable 1591 // Will maybe document later

namespace Silverback.Messaging.Messages.Statistics
{
    [SuppressMessage("ReSharper", "SA1600", Justification = "Will maybe document later")]
    public class WindowStatistics
    {
        [JsonProperty("min")]
        public long Min { get; set; }

        [JsonProperty("max")]
        public long Max { get; set; }

        [JsonProperty("avg")]
        public long Avg { get; set; }

        [JsonProperty("sum")]
        public long Sum { get; set; }

        [JsonProperty("stddev")]
        public long StdDev { get; set; }

        [JsonProperty("p50")]
        public long P50 { get; set; }

        [JsonProperty("p75")]
        public long P75 { get; set; }

        [JsonProperty("p90")]
        public long P90 { get; set; }

        [JsonProperty("p95")]
        public long P95 { get; set; }

        [JsonProperty("p99")]
        public long P99 { get; set; }

        [JsonProperty("p99_99")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Named after the JSON field")]
        [SuppressMessage("", "CA1707", Justification = "Named after the JSON field")]
        public long P99_99 { get; set; }

        [JsonProperty("outofrange")]
        public long OutOfRange { get; set; }

        [JsonProperty("hdrsize")]
        public long HdrSize { get; set; }

        [JsonProperty("cnt")]
        public long Cnt { get; set; }
    }
}
