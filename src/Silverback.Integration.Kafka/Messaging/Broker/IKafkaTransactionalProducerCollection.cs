// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Transactions;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Return a <see cref="KafkaProducer" /> that can be used to produce messages in a transaction. These producer are used through the
///     <see cref="KafkaTransactionalProducer" />.
/// </summary>
public interface IKafkaTransactionalProducerCollection : IReadOnlyCollection<KafkaProducer>
{
    /// <summary>
    ///     Gets or creates a <see cref="KafkaProducer" /> for the specified endpoint and transaction.
    /// </summary>
    /// <param name="name">
    ///     The endpoint name. It could be either the topic/queue name or the friendly name.
    /// </param>
    /// <param name="configuration">
    ///     The <see cref="KafkaProducerConfiguration" /> containing the endpoint configuration.
    /// </param>
    /// <param name="envelope">
    ///     The <see cref="IOutboundEnvelope" /> containing the message to be produced.
    /// </param>
    /// <param name="transaction">
    ///     The <see cref="IKafkaTransaction" /> to be used to produce the messages.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaProducer" />.
    /// </returns>
    ValueTask<KafkaProducer> GetOrCreateAsync(
        string name,
        KafkaProducerConfiguration configuration,
        IOutboundEnvelope envelope,
        IKafkaTransaction transaction);
}
