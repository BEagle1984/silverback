// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.KafkaEvents.Statistics;

namespace Silverback.Messaging.KafkaEvents
{
    /// <summary>
    ///     Defines the handlers for the Kafka events such as partitions revoked/assigned, error, statistics and
    ///     offsets committed.
    /// </summary>
    public class KafkaProducerEventsHandlers
    {
        /// <summary>
        ///     <para>
        ///         Gets or sets the handler to call on statistics events.
        ///     </para>
        ///     <para>
        ///         You can enable statistics and set the statistics interval using the <c>StatisticsIntervalMs</c>
        ///         configuration property (disabled by default).
        ///     </para>
        /// </summary>
        public Func<KafkaStatistics, string, KafkaProducer, Task>? StatisticsHandler { get; set; }
    }
}
