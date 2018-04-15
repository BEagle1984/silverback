using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// A Apache Kafka bases <see cref="IProducer" />
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Broker.Producer" />
    /// <seealso cref="Silverback.Messaging.Broker.IProducer" />
    public class KafkaProducer : Producer
    {
        private readonly ISerializingProducer<byte[], byte[]> _producer;
        private readonly KafkaBroker _broker;
        private readonly string _topic;

        /// <summary>
        /// Kafka producer constructor
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="context"></param>
        public KafkaProducer(IEndpoint endpoint , ISerializingProducer<byte[], byte[]> context)
            : base(endpoint)
        {
            _broker = endpoint.GetBroker<KafkaBroker>();
            _producer = context;
            _topic = endpoint.Name;
        }

        /// <summary>
        /// Sends the specified message through the message broker.
        /// </summary>
        /// <param name="message">The original message.</param>
        /// <param name="serializedMessage"> The serialized <see cref="T:Silverback.Messaging.Messages.IEnvelope" />  including the <see cref="T:Silverback.Messaging.Messages.IIntegrationMessage" />.
        /// </param>
        protected override void Produce(IIntegrationMessage message, byte[] serializedMessage)
        {
            if(_broker.Publishers.TryGetValue(_topic, out var handler))
            {
                _producer.ProduceAsync(_topic, KeyHelper.GetMessageKey(message), serializedMessage, handler.Invoke());
            }
            else
            {
                var deliveryReport = _producer.ProduceAsync(_topic, KeyHelper.GetMessageKey(message), serializedMessage).Result;
                if (deliveryReport.Error.HasError) throw new Exception(deliveryReport.Error.Reason);
            }
        }
    }
}
