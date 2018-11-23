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
        public KafkaBroker(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        protected override Producer InstantiateProducer(IEndpoint endpoint) =>
            new KafkaProducer(this, (KafkaEndpoint) endpoint);

        protected override Consumer InstantiateConsumer(IEndpoint endpoint) =>
            new KafkaConsumer(this, (KafkaEndpoint) endpoint);

        protected override void Connect(IEnumerable<IConsumer> consumers) =>
            consumers.Cast<KafkaConsumer>().ToList().ForEach(c => c.Connect());

        // TODO: Check this! Not implemented anymore in the base Broker
        //protected override void Connect(IEnumerable<IProducer> producers)
        //    => producers.Cast<KafkaProducer>().ToList().ForEach(c => c.Connect());

        protected override void Disconnect(IEnumerable<IConsumer> consumers) =>
            consumers.Cast<KafkaConsumer>().ToList().ForEach(c => c.Disconnect());

        // TODO: Check this! Not implemented anymore in the base Broker
        //protected override void Disconnect(IEnumerable<IProducer> producer)
        //    => producer.Cast<KafkaProducer>().ToList().ForEach(c => c.Disconnect());
    }
}
