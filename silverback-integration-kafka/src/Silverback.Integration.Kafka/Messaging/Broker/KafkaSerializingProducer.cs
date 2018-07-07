using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc>
    ///     <cref>Confluent.Kafka.Producer{System.Byte[], System.Byte[]}</cref>
    /// </inheritdoc>
    /// <summary>
    ///   Wrapper of Confluent Kafka Producer with byte array serializer.  
    /// </summary>
    public class KafkaSerializingProducer : Producer<byte[], byte[]>, IKafkaSerializingProducer
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaSerializingProducer"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public KafkaSerializingProducer(IEnumerable<KeyValuePair<string, object>> config) :
            base(config, new ByteArraySerializer(), new ByteArraySerializer())
        { 
        }
    }
}
