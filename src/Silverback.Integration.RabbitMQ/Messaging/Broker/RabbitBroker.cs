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
    ///     An <see cref="IBroker" /> implementation for RabbitMQ.
    /// </summary>
    /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}" />
    public class RabbitBroker : Broker<RabbitProducerEndpoint, RabbitConsumerEndpoint>
    {
        private readonly IRabbitConnectionFactory _connectionFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly MessageLogger _messageLogger;

        public RabbitBroker(
            IEnumerable<IBrokerBehavior> behaviors,
            IRabbitConnectionFactory connectionFactory,
            ILoggerFactory loggerFactory,
            MessageLogger messageLogger,
            IServiceProvider serviceProvider)
            : base(behaviors, loggerFactory, serviceProvider)
        {
            _loggerFactory = loggerFactory;
            _messageLogger = messageLogger;
            _connectionFactory = connectionFactory;
        }

        protected override IProducer InstantiateProducer(
            RabbitProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior> behaviors,
            IServiceProvider serviceProvider) =>
            new RabbitProducer(
                this,
                endpoint,
                behaviors,
                _connectionFactory,
                _loggerFactory.CreateLogger<RabbitProducer>(),
                _messageLogger);

        protected override IConsumer InstantiateConsumer(
            RabbitConsumerEndpoint endpoint,
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            IServiceProvider serviceProvider) =>
            new RabbitConsumer(
                this,
                endpoint,
                behaviors,
                _connectionFactory,
                serviceProvider,
                _loggerFactory.CreateLogger<RabbitConsumer>());

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            _connectionFactory?.Dispose();
        }
    }
}