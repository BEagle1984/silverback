using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Logging;
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

        private static ConcurrentDictionary<Dictionary<string, object>, Producer<byte[], byte[]>> _producersChache =
            new ConcurrentDictionary<Dictionary<string, object>, Producer<byte[], byte[]>>(new ConfigurationComparer());
        

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaProducer"/> class.
        /// </summary>
        /// <param name="broker">The broker.</param>
        /// <param name="endpoint">The endpoint.</param>
        public KafkaProducer(IBroker broker, KafkaEndpoint endpoint) : base(broker, endpoint)
        {
            _endpoint = endpoint;
        }

        internal void Connect()
        {
            if (_producer != null) return;
            
            _producer = _producersChache.GetOrAdd( _endpoint.Configuration ,new Producer<byte[], byte[]>(
                _endpoint.Configuration,
                    new ByteArraySerializer(),
                    new ByteArraySerializer()));
        }

        internal void Disconnect()
        {
            _producersChache.ToList().ForEach(x => x.Value?.Dispose());
            _producer = null;
        }

        /// <inheritdoc />
        protected override void Produce(IIntegrationMessage message, byte[] serializedMessage)
            => ProduceAsync(message, serializedMessage).RunSynchronously();

        /// <inheritdoc />
        protected override async Task ProduceAsync(IIntegrationMessage message, byte[] serializedMessage)
        {
            var msg = await _producer.ProduceAsync(_endpoint.Name, KeyHelper.GetMessageKey(message), serializedMessage);
            if(msg.Error.HasError) throw new Exception(msg.Error.Reason);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Disconnect();

            base.Dispose(disposing);
        }
    }

}
