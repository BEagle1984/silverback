// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Kafka;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Producer{TBroker,TEndpoint}" />
    public class KafkaTransactionalProducer : KafkaProducer
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaTransactionalProducer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that instantiated this producer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to produce to.
        /// </param>
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
        /// </param>
        /// <param name="producersCache">
        ///     The <see cref="IConfluentProducersCache" />.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="IOutboundLogger{TCategoryName}" />.
        /// </param>
        public KafkaTransactionalProducer(
            KafkaBroker broker,
            KafkaProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IConfluentProducersCache producersCache,
            IServiceProvider serviceProvider,
            IOutboundLogger<KafkaProducer> logger)
            : base(broker, endpoint, behaviorsProvider, producersCache, serviceProvider, logger)
        {
        }

        /// <summary>
        ///     <para>
        ///         Initialize the transactions.
        ///     </para>
        ///     <para>
        ///         This function ensures any transactions initiated by previous instances of the producer with
        ///         the same TransactionalId are completed. If the previous instance failed with a transaction in
        ///         progress the previous transaction will be aborted.
        ///     </para>
        ///     <para>
        ///         This function needs to be called before any other transactional or
        ///         produce functions are called when the TransactionalId is configured.
        ///     </para>
        /// </summary>
        public void InitTransaction()
        {
            IProducer<byte[]?, byte[]?> confluentProducer = GetConfluentProducer();
            confluentProducer.InitTransactions(Endpoint.Configuration.TransactionInitTimeout);
        }

        /// <summary>
        ///     Begins a new transaction.
        /// </summary>
        public void BeginTransaction()
        {
            IProducer<byte[]?, byte[]?> confluentProducer = GetConfluentProducer();
            confluentProducer.BeginTransaction();
        }

        /// <summary>
        ///     Commits the pending transaction.
        /// </summary>
        public void CommitTransaction()
        {
            IProducer<byte[]?, byte[]?> confluentProducer = GetConfluentProducer();
            confluentProducer.CommitTransaction();
        }

        /// <summary>
        ///     Aborts the pending transaction.
        /// </summary>
        public void AbortTransaction()
        {
            IProducer<byte[]?, byte[]?> confluentProducer = GetConfluentProducer();
            confluentProducer.AbortTransaction();
        }
    }
}
