// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class InMemoryBroker : Broker
    {
        private readonly MessageIdProvider _messageIdProvider;
        private readonly MessageLogger _messageLogger;

        private readonly ConcurrentDictionary<string, InMemoryTopic> _topics =
            new ConcurrentDictionary<string, InMemoryTopic>();

        public InMemoryBroker(
            MessageIdProvider messageIdProvider,
            IEnumerable<IBrokerBehavior> behaviors,
            ILoggerFactory loggerFactory,
            MessageLogger messageLogger)
            : base(behaviors, loggerFactory)
        {
            _messageIdProvider = messageIdProvider;
            _messageLogger = messageLogger;
        }

        internal InMemoryTopic GetTopic(string name) =>
            _topics.GetOrAdd(name, _ => new InMemoryTopic(name));

        /// <inheritdoc cref="Broker" />
        protected override IProducer InstantiateProducer(
            IProducerEndpoint endpoint,
            IEnumerable<IProducerBehavior> behaviors) =>
            new InMemoryProducer(
                this,
                endpoint,
                _messageIdProvider,
                behaviors,
                LoggerFactory.CreateLogger<InMemoryProducer>(),
                _messageLogger);

        /// <inheritdoc cref="Broker" />
        protected override IConsumer InstantiateConsumer(
            IConsumerEndpoint endpoint,
            IEnumerable<IConsumerBehavior> behaviors) =>
            GetTopic(endpoint.Name).Subscribe(new InMemoryConsumer(this, endpoint, behaviors));

        /// <inheritdoc cref="Broker" />
        protected override void Disconnect(IEnumerable<IConsumer> consumers)
        {
            base.Disconnect(consumers);

            _topics.Clear();
        }
    }
}