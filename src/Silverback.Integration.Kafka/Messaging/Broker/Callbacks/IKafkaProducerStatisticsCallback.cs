// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker.Callbacks.Statistics;
using Silverback.Messaging.Broker.Kafka;

namespace Silverback.Messaging.Broker.Callbacks;

/// <summary>
///     Declares the <see cref="OnProducerStatistics" /> event handler.
/// </summary>
public interface IKafkaProducerStatisticsCallback : IBrokerClientCallback
{
    /// <summary>
    ///     Called on statistics events.
    /// </summary>
    /// <remarks>
    ///     You can enable statistics and set the statistics interval using the <c>StatisticsIntervalMs</c>
    ///     configuration property (disabled by default).
    /// </remarks>
    /// <param name="statistics">
    ///     The deserialized statistics.
    /// </param>
    /// <param name="rawStatistics">
    ///     The raw statistics string.
    /// </param>
    /// <param name="producerWrapper">
    ///     The related <see cref="IConfluentProducerWrapper" />.
    /// </param>
    void OnProducerStatistics(KafkaStatistics? statistics, string rawStatistics, IConfluentProducerWrapper producerWrapper);
}
