using Newtonsoft.Json;

namespace Silverback.Messaging.Messages.Statistics
{
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
        public long P99_99 { get; set; }

        [JsonProperty("outofrange")]
        public long OutOfRange { get; set; }

        [JsonProperty("hdrsize")]
        public long HdrSize { get; set; }

        [JsonProperty("cnt")]
        public long Cnt { get; set; }
    }
}