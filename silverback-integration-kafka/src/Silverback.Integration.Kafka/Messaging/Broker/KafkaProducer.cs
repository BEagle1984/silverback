using Silverback.Messaging.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        private IKafkaSerializingProducer _producer;

        private readonly Func<IKafkaSerializingProducer> _producerFactory; 

        private static readonly ConcurrentDictionary<Dictionary<string, object>, IKafkaSerializingProducer> Producers =
            new ConcurrentDictionary<Dictionary<string, object>, IKafkaSerializingProducer>(new ConfigurationComparer());

        private static readonly ConcurrentDictionary<Dictionary<string, object>, int> ProducersInstanceCounter =
            new ConcurrentDictionary<Dictionary<string, object>, int>(new ConfigurationComparer());

        private static readonly object SyncLock = new object();

        /// <inheritdoc /> 
        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaProducer"/> class.
        /// </summary>
        /// <param name="broker">The broker.</param>
        /// <param name="endpoint">The endpoint.</param>
        public KafkaProducer(IBroker broker, KafkaEndpoint endpoint) : base(broker, endpoint)
        {
            _endpoint = endpoint;
            _producerFactory =  () => new KafkaSerializingProducer(_endpoint.Configuration);
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaProducer"/> class.
        /// </summary>
        /// <param name="broker">The broker.</param>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="producerFactory">The KafkaSerializerProducer factory.</param>
        public KafkaProducer(IBroker broker, KafkaEndpoint endpoint, Func<IKafkaSerializingProducer> producerFactory) : base(broker, endpoint)
        {
            _endpoint = endpoint;
            _producerFactory = producerFactory;
        }

        /// <summary>
        /// Create, if not already exist, a new istance of producer and cache it by configuration.
        /// </summary>
        internal void Connect()
        {
            lock (SyncLock)
            {
                if (Producers.ContainsKey(_endpoint.Configuration))
                {
                    ProducersInstanceCounter.TryGetValue(_endpoint.Configuration, out var producerInstanceCounter);
                    ProducersInstanceCounter.TryUpdate(_endpoint.Configuration, producerInstanceCounter++, producerInstanceCounter);
                    Producers.TryGetValue(_endpoint.Configuration, out _producer);
                    return;
                }
                ProducersInstanceCounter.GetOrAdd(_endpoint.Configuration, 1);
                _producer = Producers.GetOrAdd(_endpoint.Configuration, _producerFactory.Invoke());
            }
        }

        internal void Disconnect()
        {
            lock (SyncLock)
            {
                ProducersInstanceCounter.TryGetValue(_endpoint.Configuration, out var count);
                if (count == 1)
                {
                    Producers.TryRemove(_endpoint.Configuration, out var removedItem);
                    ProducersInstanceCounter.TryRemove(_endpoint.Configuration, out var removed);
                    removedItem?.Dispose();
                }
                ProducersInstanceCounter.TryUpdate(_endpoint.Configuration, count--, count);
            }
        }
        
        /// <inheritdoc />
        protected override void Produce(IIntegrationMessage message, byte[] serializedMessage)
            => Task.Run(() => ProduceAsync(message, serializedMessage)).Wait();

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
