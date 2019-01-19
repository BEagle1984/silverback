// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// A <see cref="Broker"/> implementation for Apache Kafka.
    /// </summary>
    public class KafkaBroker : Broker<KafkaEndpoint>
    {
        private readonly MessageKeyProvider _messageKeyProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly MessageLogger _messageLogger;

        public KafkaBroker(MessageKeyProvider messageKeyProvider, ILoggerFactory loggerFactory,
            MessageLogger messageLogger) : base(loggerFactory)
        {
            _messageKeyProvider = messageKeyProvider;
            _loggerFactory = loggerFactory;
            _messageLogger = messageLogger;
        }

        protected override Producer InstantiateProducer(IEndpoint endpoint) =>
            new KafkaProducer(this, (KafkaProducerEndpoint) endpoint, _messageKeyProvider, _loggerFactory.CreateLogger<KafkaProducer>(), _messageLogger);

        protected override Consumer InstantiateConsumer(IEndpoint endpoint) =>
            new KafkaConsumer(this, (KafkaConsumerEndpoint) endpoint, _loggerFactory.CreateLogger<KafkaConsumer>(), _messageLogger);

        protected override void Connect(IEnumerable<IConsumer> consumers) =>
            consumers.Cast<KafkaConsumer>().ToList().ForEach(c => c.Connect());

        protected override void Disconnect(IEnumerable<IConsumer> consumers) =>
            consumers.Cast<KafkaConsumer>().ToList().ForEach(c => c.Disconnect());
    }
}
