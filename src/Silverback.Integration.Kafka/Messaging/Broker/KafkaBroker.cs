// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     An <see cref="IBroker" /> implementation for Apache Kafka.
    /// </summary>
    public class KafkaBroker : Broker<KafkaProducerEndpoint, KafkaConsumerEndpoint>
    {
        private readonly MessageIdProvider _messageIdProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly MessageLogger _messageLogger;

        public KafkaBroker(
            MessageIdProvider messageIdProvider,
            IEnumerable<IBrokerBehavior> behaviors,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            MessageLogger messageLogger)
            : base(behaviors, loggerFactory)
        {
            _messageIdProvider = messageIdProvider;
            _serviceProvider = serviceProvider;
            _loggerFactory = loggerFactory;
            _messageLogger = messageLogger;
        }

        /// <inheritdoc cref="Broker" />
        protected override IProducer InstantiateProducer(
            KafkaProducerEndpoint endpoint,
            IEnumerable<IProducerBehavior> behaviors) =>
            new KafkaProducer(
                this,
                endpoint,
                _messageIdProvider,
                behaviors,
                _loggerFactory.CreateLogger<KafkaProducer>(),
                _messageLogger,
                _serviceProvider);

        /// <inheritdoc cref="Broker" />
        protected override IConsumer InstantiateConsumer(
            KafkaConsumerEndpoint endpoint,
            IEnumerable<IConsumerBehavior> behaviors) =>
            new KafkaConsumer(
                this,
                endpoint,
                behaviors,
                _serviceProvider,
                _loggerFactory.CreateLogger<KafkaConsumer>());
    }
}