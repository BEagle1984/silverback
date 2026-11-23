// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Confluent.Kafka;

namespace Silverback.Tests.Integration.Kafka.TestTypes;

// The mock cluster built into librdkafka (test.mock.num.brokers), which runs in process and listens on the loopback interface. It's
// owned by a producer, whose metadata exposes the address of the mock broker to the other clients.
public sealed class LibrdkafkaMockCluster : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly IProducer<byte[]?, byte[]?> _producer;

    public LibrdkafkaMockCluster()
    {
        ProducerConfig config = new();
        config.Set("test.mock.num.brokers", "1");
        _producer = new ProducerBuilder<byte[]?, byte[]?>(config).Build();

        using IAdminClient adminClient = new DependentAdminClientBuilder(_producer.Handle).Build();
        BrokerMetadata broker = adminClient.GetMetadata(Timeout).Brokers.Single();
        BootstrapServers = $"{broker.Host}:{broker.Port}";
    }

    public string BootstrapServers { get; }

    // The topic is created when the first message is produced to it. The delivery is awaited synchronously, since the continuations
    // of ProduceAsync would run in the thread polling the producer.
    public void Produce(string topic) => Produce(new TopicPartition(topic, Partition.Any));

    public void Produce(TopicPartition topicPartition)
    {
        Error? deliveryError = null;

        _producer.Produce(topicPartition, new Message<byte[]?, byte[]?> { Value = [1, 2, 3] }, report => deliveryError = report.Error);

        if (_producer.Flush(Timeout) > 0)
            throw new TimeoutException("The message was not delivered within the timeout.");

        if (deliveryError is { IsError: true })
            throw new KafkaException(deliveryError);
    }

    public void Dispose() => _producer.Dispose();
}
