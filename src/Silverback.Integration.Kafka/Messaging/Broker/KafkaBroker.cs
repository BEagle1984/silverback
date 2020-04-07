// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     An <see cref="IBroker" /> implementation for Apache Kafka.
    /// </summary>
    /// <inheritdoc cref="Broker" />
    public class KafkaBroker : Broker<KafkaProducerEndpoint, KafkaConsumerEndpoint>
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly MessageLogger _messageLogger;

        public KafkaBroker(
            IEnumerable<IBrokerBehavior> behaviors,
            ILoggerFactory loggerFactory,
            MessageLogger messageLogger,
            IServiceProvider serviceProvider)
            : base(behaviors, loggerFactory, serviceProvider)
        {
            _loggerFactory = loggerFactory;
            _messageLogger = messageLogger;
        }

        protected override IProducer InstantiateProducer(
            KafkaProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior> behaviors,
            IServiceProvider serviceProvider) =>
            new KafkaProducer(
                this,
                endpoint,
                behaviors,
                _messageLogger,
                serviceProvider,
                _loggerFactory.CreateLogger<KafkaProducer>());

        protected override IConsumer InstantiateConsumer(
            KafkaConsumerEndpoint endpoint,
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            IServiceProvider serviceProvider) =>
            new KafkaConsumer(
                this,
                endpoint,
                behaviors,
                serviceProvider,
                _loggerFactory.CreateLogger<KafkaConsumer>());
    }
}