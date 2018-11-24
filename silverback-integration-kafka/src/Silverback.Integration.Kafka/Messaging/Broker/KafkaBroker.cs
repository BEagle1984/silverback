using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// A <see cref="Broker"/> implementation for Apache Kafka.
    /// </summary>
    public class KafkaBroker : Broker
    {
        private readonly ILoggerFactory _loggerFactory;

        public KafkaBroker(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        protected override Producer InstantiateProducer(IEndpoint endpoint) =>
            new KafkaProducer(this, (KafkaEndpoint) endpoint, _loggerFactory.CreateLogger<KafkaProducer>());

        protected override Consumer InstantiateConsumer(IEndpoint endpoint) =>
            new KafkaConsumer(this, (KafkaEndpoint) endpoint, _loggerFactory.CreateLogger<KafkaConsumer>());

        protected override void Connect(IEnumerable<IConsumer> consumers) =>
            consumers.Cast<KafkaConsumer>().ToList().ForEach(c => c.Connect());

        protected override void Disconnect(IEnumerable<IConsumer> consumers) =>
            consumers.Cast<KafkaConsumer>().ToList().ForEach(c => c.Disconnect());
    }
}
