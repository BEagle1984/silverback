// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     The <see cref="IProducer{TKey,TValue}" /> builder used by the <see cref="KafkaProducer" />.
/// </summary>
public interface IConfluentProducerBuilder
{
    /// <summary>
    ///     Sets the producer configuration.
    /// </summary>
    /// <param name="config">
    ///     The configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="IConfluentProducerBuilder" /> so that additional calls can be chained.
    /// </returns>
    IConfluentProducerBuilder SetConfiguration(ProducerConfig config);

    /// <summary>
    ///     Sets the handler to call on statistics events.
    /// </summary>
    /// <param name="statisticsHandler">
    ///     The event handler.
    /// </param>
    /// <returns>
    ///     The <see cref="IConfluentProducerBuilder" /> so that additional calls can be chained.
    /// </returns>
    IConfluentProducerBuilder SetStatisticsHandler(Action<IProducer<byte[]?, byte[]?>, string> statisticsHandler);

    /// <summary>
    ///     Set the handler to call when there is information available to be logged. If not specified, a default
    ///     callback that writes to stderr will be used.
    /// </summary>
    /// <param name="logHandler">
    ///     The event handler.
    /// </param>
    /// <returns>
    ///     The <see cref="IConfluentProducerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public IConfluentProducerBuilder SetLogHandler(Action<IProducer<byte[]?, byte[]?>, LogMessage> logHandler);

    /// <summary>
    ///     Builds the <see cref="IProducer{TKey,TValue}" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IProducer{TKey,TValue}" />.
    /// </returns>
    IProducer<byte[]?, byte[]?> Build();
}
