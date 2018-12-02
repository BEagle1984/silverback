namespace Silverback.Messaging.Broker
{
    internal delegate void MessageReceivedHandler(Confluent.Kafka.Message<byte[], byte[]> message, Confluent.Kafka.TopicPartitionOffset tpo, int retryCount);
}