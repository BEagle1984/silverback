using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc />
    public class KafkaBroker : Broker
    {
        public override Producer GetNewProducer(IEndpoint endpoint)
            => new KafkaProducer(this, (KafkaEndpoint)endpoint);

        public override Consumer GetNewConsumer(IEndpoint endpoint)
            => new KafkaConsumer(this, (KafkaEndpoint)endpoint);

        protected override void Connect(IEnumerable<IConsumer> consumers) 
            => consumers.Cast<KafkaConsumer>().ToList().ForEach(c => c.Connect());

        protected override void Connect(IEnumerable<IProducer> producers)
            => producers.Cast<KafkaProducer>().ToList().ForEach(c => c.Connect());

        protected override void Disconnect(IEnumerable<IConsumer> consumers)
            => consumers.Cast<KafkaConsumer>().ToList().ForEach(c => c.Disconnect());

        protected override void Disconnect(IEnumerable<IProducer> producer)
            => producer.Cast<KafkaProducer>().ToList().ForEach(c => c.Disconnect());
    }
}
