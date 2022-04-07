// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     An <see cref="IBroker" /> implementation for Apache Kafka.
    /// </summary>
    public class KafkaBroker : Broker<KafkaProducerEndpoint, KafkaConsumerEndpoint>
    {
        private readonly IServiceProvider _serviceProvider;

        private List<KafkaProducerEndpoint>? _transactionalEndpoints = new();

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaBroker" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        public KafkaBroker(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        }

        /// <summary>
        ///     Returns a <see cref="KafkaTransactionalProducer" /> to be used to produce to the specified
        ///     endpoint using a transaction.
        /// </summary>
        /// <param name="endpoint">
        ///     The target endpoint.
        /// </param>
        /// <returns>
        ///     The <see cref="IProducer" /> for the specified endpoint.
        /// </returns>
        public KafkaTransactionalProducer GetTransactionalProducer(IProducerEndpoint endpoint)
        {
            KafkaProducerEndpoint kafkaEndpoint = (KafkaProducerEndpoint)endpoint;

            if (_transactionalEndpoints == null)
                throw new ObjectDisposedException(GetType().FullName);

            if (!_transactionalEndpoints.Contains(kafkaEndpoint))
                _transactionalEndpoints.Add(kafkaEndpoint);

            KafkaTransactionalProducer producer =
                (KafkaTransactionalProducer)InstantiateProducer(
                    kafkaEndpoint,
                    _serviceProvider.GetRequiredService<IBrokerBehaviorsProvider<IProducerBehavior>>(),
                    _serviceProvider);

            producer.InitTransaction();

            return producer;
        }

        /// <summary>
        ///     Returns an existing <see cref="IProducer" /> to be used to produce to the specified endpoint.
        /// </summary>
        /// <param name="endpointName">
        ///     The target endpoint name (or friendly name).
        /// </param>
        /// <returns>
        ///     The <see cref="IProducer" /> for the specified endpoint.
        /// </returns>
        public KafkaTransactionalProducer GetTransactionalProducer(string endpointName) =>
            (KafkaTransactionalProducer)GetProducer(endpointName);

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.GetProducer(IProducerEndpoint)" />
        public override IProducer GetProducer(IProducerEndpoint endpoint)
        {
            KafkaProducerEndpoint kafkaEndpoint = (KafkaProducerEndpoint)Check.NotNull(endpoint, nameof(endpoint));

            return kafkaEndpoint.Configuration.IsTransactional
                ? GetTransactionalProducer(kafkaEndpoint)
                : base.GetProducer(endpoint);
        }

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.GetProducer(string)" />
        public override IProducer GetProducer(string endpointName)
        {
            Check.NotEmpty(endpointName, nameof(endpointName));

            if (_transactionalEndpoints == null)
                throw new ObjectDisposedException(GetType().FullName);

            var transactionalEndpoint = _transactionalEndpoints.FirstOrDefault(
                endpoint => endpoint.Name == endpointName ||
                            endpoint.FriendlyName == endpointName);

            return transactionalEndpoint == null
                ? base.GetProducer(endpointName)
                : GetTransactionalProducer(transactionalEndpoint);
        }

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateProducer" />
        protected override IProducer InstantiateProducer(
            KafkaProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider)
        {
            Check.NotNull(endpoint, nameof(endpoint));

            if (endpoint.Configuration.IsTransactional)
            {
                return new KafkaTransactionalProducer(
                    this,
                    endpoint,
                    behaviorsProvider,
                    serviceProvider.GetRequiredService<IConfluentProducersCache>(),
                    serviceProvider,
                    serviceProvider.GetRequiredService<IOutboundLogger<KafkaProducer>>());
            }

            return new KafkaProducer(
                this,
                endpoint,
                behaviorsProvider,
                serviceProvider.GetRequiredService<IConfluentProducersCache>(),
                serviceProvider,
                serviceProvider.GetRequiredService<IOutboundLogger<KafkaProducer>>());
        }

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.InstantiateConsumer" />
        protected override IConsumer InstantiateConsumer(
            KafkaConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider) =>
            new KafkaConsumer(
                this,
                endpoint,
                behaviorsProvider,
                serviceProvider.GetRequiredService<IConfluentConsumerBuilder>(),
                serviceProvider.GetRequiredService<IBrokerCallbacksInvoker>(),
                serviceProvider,
                serviceProvider.GetRequiredService<IInboundLogger<KafkaConsumer>>());

        /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}.Dispose(bool)" />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _transactionalEndpoints = null;

            base.Dispose(disposing);
        }
    }
}
