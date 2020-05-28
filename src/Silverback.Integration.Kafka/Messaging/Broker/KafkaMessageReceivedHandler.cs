// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker
{
    internal delegate Task KafkaMessageReceivedHandler(
        Message<byte[], byte[]> message,
        TopicPartitionOffset topicPartitionOffset);
}
