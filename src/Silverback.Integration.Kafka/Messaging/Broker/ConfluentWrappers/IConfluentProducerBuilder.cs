// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.ConfluentWrappers
{
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
        IConfluentProducerBuilder SetConfig(ProducerConfig config);

        /// <summary>
        ///     Sets the handler to call on statistics events.
        /// </summary>
        /// <param name="statisticsHandler">
        ///     The event handler.
        /// </param>
        /// <returns>
        ///     The <see cref="IConfluentProducerBuilder" /> so that additional calls can be chained.
        /// </returns>
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        IConfluentProducerBuilder SetStatisticsHandler(Action<IProducer<byte[]?, byte[]?>, string> statisticsHandler);

        /// <summary>
        ///     Builds the <see cref="IProducer{TKey,TValue}" /> instance.
        /// </summary>
        /// <returns>
        ///     The <see cref="IProducer{TKey,TValue}" />.
        /// </returns>
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        IProducer<byte[]?, byte[]?> Build();
    }
}
