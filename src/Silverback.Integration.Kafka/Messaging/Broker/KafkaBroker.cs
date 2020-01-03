// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly MessageLogger _messageLogger;

        public KafkaBroker(
            MessageKeyProvider messageKeyProvider,
            IEnumerable<IBrokerBehavior> behaviors,
            IServiceProvider serviceProvider, 
            ILoggerFactory loggerFactory, 
            MessageLogger messageLogger)
            : base(behaviors, loggerFactory)
        {
            _messageKeyProvider = messageKeyProvider;
            _serviceProvider = serviceProvider;
            _loggerFactory = loggerFactory;
            _messageLogger = messageLogger;
        }

        protected override Producer InstantiateProducer(IEndpoint endpoint, IEnumerable<IProducerBehavior> behaviors) =>
            new KafkaProducer(
                this,
                (KafkaProducerEndpoint) endpoint,
                _messageKeyProvider,
                behaviors,
                _loggerFactory.CreateLogger<KafkaProducer>(),
                _messageLogger);

        protected override Consumer InstantiateConsumer(IEndpoint endpoint, IEnumerable<IConsumerBehavior> behaviors) =>
            new KafkaConsumer(
                this,
                (KafkaConsumerEndpoint) endpoint,
                behaviors,
                _serviceProvider,
                _loggerFactory.CreateLogger<KafkaConsumer>());

        protected override void Connect(IEnumerable<IConsumer> consumers) =>
            consumers.Cast<KafkaConsumer>().ToList().ForEach(c => c.Connect());

        protected override void Disconnect(IEnumerable<IConsumer> consumers) =>
            consumers.Cast<KafkaConsumer>().ToList().ForEach(c => c.Disconnect());
    }
}
