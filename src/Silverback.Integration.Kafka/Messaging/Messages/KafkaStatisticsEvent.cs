// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Newtonsoft.Json;
using Silverback.Messaging.Messages.Statistics;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The event published when statistics are received. Statistics are provided as a JSON formatted string
    ///     as defined here: https://github.com/edenhill/librdkafka/blob/master/STATISTICS.md and also as
    ///     <see cref="KafkaStatistics" /> object.
    /// </summary>
    /// <remarks>
    ///     You can enable statistics and set the statistics interval using the <c>
    ///         StatisticsIntervalMs
    ///     </c> configuration parameter (disabled by default).
    /// </remarks>
    public class KafkaStatisticsEvent : IKafkaEvent
    {
        private KafkaStatistics? _statistics;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaStatisticsEvent" /> class.
        /// </summary>
        /// <param name="rawJson">
        ///     The raw statistics JSON as received from the underlying librdkafka.
        /// </param>
        public KafkaStatisticsEvent(string rawJson)
        {
            Raw = rawJson;
        }

        /// <summary>
        ///     Gets the statistics in raw JSON, as they are provided from the underlining librdkafka (see
        ///     https://github.com/edenhill/librdkafka/blob/master/STATISTICS.md).
        /// </summary>
        public string Raw { get; }

        /// <summary>
        ///     Gets the statistics as typed <see cref="KafkaStatistics" /> object.
        /// </summary>
        public KafkaStatistics Statistics => _statistics ??= JsonConvert.DeserializeObject<KafkaStatistics>(Raw);
    }
}
