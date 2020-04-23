// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Broker" />
    public class InMemoryBroker : Broker<IProducerEndpoint, IConsumerEndpoint>
    {
        private readonly MessageLogger _messageLogger;

        private readonly ConcurrentDictionary<string, InMemoryTopic> _topics =
            new ConcurrentDictionary<string, InMemoryTopic>();

        public InMemoryBroker(
            IEnumerable<IBrokerBehavior> behaviors,
            ILoggerFactory loggerFactory,
            MessageLogger messageLogger,
            IServiceProvider serviceProvider)
            : base(behaviors, loggerFactory, serviceProvider)
        {
            _messageLogger = messageLogger;
        }

        internal InMemoryTopic GetTopic(string name) =>
            _topics.GetOrAdd(name, _ => new InMemoryTopic(name));

        protected override IProducer InstantiateProducer(
            IProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior> behaviors,
            IServiceProvider serviceProvider) =>
            new InMemoryProducer(
                this,
                endpoint,
                behaviors,
                LoggerFactory.CreateLogger<InMemoryProducer>(),
                _messageLogger);

        protected override IConsumer InstantiateConsumer(
            IConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            IServiceProvider serviceProvider) =>
            GetTopic(endpoint.Name).Subscribe(new InMemoryConsumer(
                this,
                endpoint,
                callback,
                behaviors,
                serviceProvider,
                LoggerFactory.CreateLogger<InMemoryConsumer>()));

        protected override void Disconnect(IEnumerable<IConsumer> consumers)
        {
            base.Disconnect(consumers);

            _topics.Clear();
        }
    }
}