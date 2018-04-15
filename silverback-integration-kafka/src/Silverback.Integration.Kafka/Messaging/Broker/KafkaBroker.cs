using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// Silverback Broker class for Apache Kafka streaming platform.
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Broker.Broker" />
    public class KafkaBroker : Broker
    {
        internal Dictionary<string, object> ProducerConfiguration;

        internal readonly ConcurrentDictionary<string, Func<IDeliveryReportHandler>> Publishers = 
            new ConcurrentDictionary<string, Func<IDeliveryReportHandler>>();

        private static Producer<byte[], byte[]> _producerContext;

        /// <inheritdoc />
        public override IConsumer GetConsumer(IEndpoint endpoint)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override IProducer GetProducer(IEndpoint endpoint)
        {
            if(_producerContext == null)
                _producerContext = new Producer<byte[], byte[]>(ProducerConfiguration,
                    new ByteArraySerializer(),
                    new ByteArraySerializer());

            return new KafkaProducer(endpoint, _producerContext);
        }

        /// <inheritdoc />
        public override void ValidateConfiguration()
        {
            var errorMessages = new List<string>();
            
            if (ProducerConfiguration.Count == 0) errorMessages.Add("Broker connection parameters not found.");

            //TODO: Add other validator
            if (errorMessages.Count > 0)
                throw new InvalidOperationException(string.Join(", ", errorMessages));
        }
    }

}
