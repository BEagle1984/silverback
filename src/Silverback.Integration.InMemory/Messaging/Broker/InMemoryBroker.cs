using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class InMemoryBroker : Broker<IEndpoint>
    {
        private readonly MessageKeyProvider _messageKeyProvider;
        private readonly MessageLogger _messageLogger;
        private readonly ConcurrentDictionary<string, InMemoryTopic> _topics = new ConcurrentDictionary<string, InMemoryTopic>();

        public InMemoryBroker(MessageKeyProvider messageKeyProvider, ILoggerFactory loggerFactory, MessageLogger messageLogger) : base(loggerFactory)
        {
            _messageKeyProvider = messageKeyProvider;
            _messageLogger = messageLogger;
        }

        internal InMemoryTopic GetTopic(string name) =>
            _topics.GetOrAdd(name, _ => new InMemoryTopic(name));

        protected override Producer InstantiateProducer(IEndpoint endpoint) =>
            new InMemoryProducer(this, endpoint, _messageKeyProvider, LoggerFactory.CreateLogger<InMemoryProducer>(), _messageLogger);

        protected override Consumer InstantiateConsumer(IEndpoint endpoint) =>
            GetTopic(endpoint.Name).Subscribe(
                new InMemoryConsumer(this, endpoint, LoggerFactory.CreateLogger<InMemoryConsumer>(), _messageLogger));

        protected override void Connect(IEnumerable<IConsumer> consumers)
        {
        }

        protected override void Disconnect(IEnumerable<IConsumer> consumers)
        {
            _topics.Clear();
        }
    }
}
