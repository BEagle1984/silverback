// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)
namespace Silverback.Messaging.Broker
{
    internal delegate void MessageReceivedHandler(Confluent.Kafka.Message<byte[], byte[]> message, Confluent.Kafka.TopicPartitionOffset tpo, int retryCount);
}