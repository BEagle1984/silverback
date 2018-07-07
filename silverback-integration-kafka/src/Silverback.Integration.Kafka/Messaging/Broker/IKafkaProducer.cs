using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker
{
    public interface IKafkaSerializingProducer : ISerializingProducer<byte[], byte[]>, IDisposable
    {
    }
}
