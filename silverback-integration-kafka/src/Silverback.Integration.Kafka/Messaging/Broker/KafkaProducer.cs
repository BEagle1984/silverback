using System;
using System.Collections.Generic;
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
        private readonly Producer<byte[], byte[]> _producer;
        private readonly string _topic;
        private readonly IDeliveryHandler<byte[], byte[]> _deliveryHandler;


        /// <summary>
        /// Kafka producer constructor
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="config"></param>
        /// <param name="handler"></param>
        public KafkaProducer(IEndpoint endpoint, Dictionary<string, object> config, IDeliveryHandler<byte[], byte[]> handler = null)
            : base(endpoint)
        {
            _topic = endpoint.Name;
            _deliveryHandler = handler;
            _producer = new Producer<byte[], byte[]>(config,
                new ByteArraySerializer(),
                new ByteArraySerializer());
        }

        /// <summary>
        /// Sends the specified message through the message broker.
        /// </summary>
        /// <param name="message">The original message.</param>
        /// <param name="serializedMessage"> The serialized <see cref="T:Silverback.Messaging.Messages.IEnvelope" />  including the <see cref="T:Silverback.Messaging.Messages.IIntegrationMessage" />.
        /// </param>
        protected override void Produce(IIntegrationMessage message, byte[] serializedMessage)
        {
            if (_deliveryHandler != null)
            {
                _producer.ProduceAsync(_topic, KeyHelper.GetMessageKey(message), serializedMessage, _deliveryHandler); 
            }
            else
            {
                var deliveryReport = _producer.ProduceAsync(_topic, KeyHelper.GetMessageKey(message), serializedMessage);
                if (deliveryReport.Result.Error.HasError) throw new Exception(deliveryReport.Result.Error.Reason);
            }
        }
        
        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            _producer?.Dispose();
        }
    }
}
