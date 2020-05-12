// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     An <see cref="IBroker" /> implementation for Apache Kafka.
    /// </summary>
    /// <inheritdoc cref="Broker" />
    public class KafkaBroker : Broker<KafkaProducerEndpoint, KafkaConsumerEndpoint>
    {
        private readonly ILoggerFactory _loggerFactory;

        public KafkaBroker(
            IEnumerable<IBrokerBehavior> behaviors,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider)
            : base(behaviors, loggerFactory, serviceProvider) =>
            _loggerFactory = loggerFactory;

        protected override IProducer InstantiateProducer(
            KafkaProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior> behaviors,
            IServiceProvider serviceProvider) =>
            new KafkaProducer(
                this,
                endpoint,
                behaviors,
                serviceProvider,
                _loggerFactory.CreateLogger<KafkaProducer>());

        protected override IConsumer InstantiateConsumer(
            KafkaConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            IServiceProvider serviceProvider) =>
            new KafkaConsumer(
                this,
                endpoint,
                callback,
                behaviors,
                serviceProvider,
                _loggerFactory.CreateLogger<KafkaConsumer>());
    }
}