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
        private readonly KafkaEndpoint _endpoint;
        private Producer<byte[], byte[]> _producer;

        /// <summary>
        /// Kafka producer constructor
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="context"></param>
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

        /// <summary>
        /// Sends the specified message through the message broker.
        /// </summary>
        /// <param name="message">The original message.</param>
        /// <param name="serializedMessage"> The serialized <see cref="T:Silverback.Messaging.Messages.IEnvelope" />  including the <see cref="T:Silverback.Messaging.Messages.IIntegrationMessage" />.
        /// </param>
        protected override void Produce(IIntegrationMessage message, byte[] serializedMessage)
        {
            var deliveryReport = _producer.ProduceAsync(_endpoint.Name, KeyHelper.GetMessageKey(message), serializedMessage).Result;
            if (deliveryReport.Error.HasError) throw new Exception(deliveryReport.Error.Reason);
        }


        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _producer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
