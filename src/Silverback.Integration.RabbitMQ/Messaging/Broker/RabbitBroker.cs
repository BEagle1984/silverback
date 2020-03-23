// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     An <see cref="IBroker" /> implementation for RabbitMQ.
    /// </summary>
    public class RabbitBroker : Broker<RabbitProducerEndpoint, RabbitConsumerEndpoint>
    {
        private readonly IRabbitConnectionFactory _connectionFactory;
        private readonly MessageIdProvider _messageIdProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly MessageLogger _messageLogger;

        public RabbitBroker(
            MessageIdProvider messageIdProvider,
            IEnumerable<IBrokerBehavior> behaviors,
            IRabbitConnectionFactory connectionFactory,
            ILoggerFactory loggerFactory,
            MessageLogger messageLogger)
            : base(behaviors, loggerFactory)
        {
            _messageIdProvider = messageIdProvider;
            _loggerFactory = loggerFactory;
            _messageLogger = messageLogger;
            _connectionFactory = connectionFactory;
        }

        /// <inheritdoc cref="Broker" />
        protected override IProducer InstantiateProducer(
            RabbitProducerEndpoint endpoint,
            IEnumerable<IProducerBehavior> behaviors) =>
            new RabbitProducer(
                this,
                endpoint,
                _messageIdProvider,
                behaviors,
                _connectionFactory,
                _loggerFactory.CreateLogger<RabbitProducer>(),
                _messageLogger);

        /// <inheritdoc cref="Broker" />
        protected override IConsumer InstantiateConsumer(
            RabbitConsumerEndpoint endpoint,
            IEnumerable<IConsumerBehavior> behaviors) =>
            new RabbitConsumer(
                this,
                endpoint,
                behaviors,
                _connectionFactory,
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