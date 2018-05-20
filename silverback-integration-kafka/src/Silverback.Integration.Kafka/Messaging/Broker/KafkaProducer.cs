using System;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc />
    /// <summary>
    /// A Apache Kafka bases <see cref="IProducer" />
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Broker.Producer" />
    /// <seealso cref="Silverback.Messaging.Broker.IProducer" />
    public class KafkaProducer : Producer
    {
        private readonly KafkaEndpoint _endpoint;
        private Producer<byte[], byte[]> _producer;

        /// <inheritdoc />
        public KafkaProducer(IBroker broker, KafkaEndpoint endpoint)
            : base(broker,endpoint)
        {
            _endpoint = endpoint;
        }

        internal void Connect()
        {
            if (_producer != null) return;

            _producer = new Producer<byte[], byte[]>(
                _endpoint.Configuration,
                    new ByteArraySerializer(),
                    new ByteArraySerializer());
        }

        /// <inheritdoc />
        protected override void Produce(IIntegrationMessage message, byte[] serializedMessage)
        {
            var deliveryReport = _producer.ProduceAsync(_endpoint.Name, KeyHelper.GetMessageKey(message), serializedMessage).Result;
            if (deliveryReport.Error.HasError) throw new Exception(deliveryReport.Error.Reason);
        }


        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _producer.Dispose();

            base.Dispose(disposing);
        }
    }
}
