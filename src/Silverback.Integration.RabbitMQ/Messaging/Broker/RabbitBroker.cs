// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// An <see cref="IBroker"/> implementation for RabbitMQ.
    /// </summary>
    public class RabbitBroker : Broker
    {
        private readonly RabbitConnectionFactory _connectionFactory = new RabbitConnectionFactory();
        private readonly MessageKeyProvider _messageKeyProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly MessageLogger _messageLogger;

        public RabbitBroker(
            MessageKeyProvider messageKeyProvider,
            IEnumerable<IBrokerBehavior> behaviors,
            ILoggerFactory loggerFactory,
            MessageLogger messageLogger) 
            : base(behaviors, loggerFactory)
        {
            _messageKeyProvider = messageKeyProvider;
            _loggerFactory = loggerFactory;
            _messageLogger = messageLogger;
        }

        /// <inheritdoc cref="Producer"/>
        protected override IProducer InstantiateProducer(
            IProducerEndpoint endpoint,
            IEnumerable<IProducerBehavior> behaviors) =>
            new RabbitProducer(
                this,
                (RabbitProducerEndpoint) endpoint,
                _messageKeyProvider,
                behaviors,
                _connectionFactory,
                _loggerFactory.CreateLogger<RabbitProducer>(),
                _messageLogger);

        /// <inheritdoc cref="Producer"/>
        protected override IConsumer InstantiateConsumer(
            IConsumerEndpoint endpoint,
            IEnumerable<IConsumerBehavior> behaviors)
        {
            throw new System.NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            
            if (!disposing)
                return;

            _connectionFactory?.Dispose();
        }
    }
}