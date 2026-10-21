// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using Dekaf.Consumer;
using Dekaf.Producer;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Tests.Integration.E2E.KafkaBroker.TestHost;

/// <summary>
///     Creates the real Dekaf clients, connected to the test container regardless of the configured bootstrap
///     servers, so that the ported tests keep their <c>WithBootstrapServers("PLAINTEXT://e2e")</c> configuration.
/// </summary>
/// <remarks>
///     A few defaults reproduce the behavior of the mocked Kafka the tests were written for: the consumers start
///     from the earliest offset (the tests produce right after connecting, before the position is resolved) and
///     the messages without key are spread in round-robin.
/// </remarks>
internal sealed class ContainerDekafClientsFactory : IDekafClientsFactory
{
    // The default 5 seconds would slow down every wait for the committed offsets
    private const int AutoCommitIntervalMs = 100;

    private readonly DekafClientsFactory _innerFactory;

    private readonly KafkaContainerHost _containerHost;

    public ContainerDekafClientsFactory(KafkaContainerHost containerHost, ILoggerFactory loggerFactory)
    {
        _containerHost = containerHost;
        _innerFactory = new DekafClientsFactory(loggerFactory, new KeylessRoundRobinPartitioner());
    }

    public IKafkaConsumer<byte[], byte[]> CreateConsumer(KafkaConsumerConfiguration configuration, IConsumerAwareRebalanceListener rebalanceListener)
    {
        _containerHost.EnsureTopicsExistAsync(
                configuration.Endpoints
                    .SelectMany(endpoint => endpoint.TopicPartitions)
                    .Select(topicPartition => topicPartition.Topic))
            .SafeWait();

        return _innerFactory.CreateConsumer(
            configuration with
            {
                BootstrapServers = _containerHost.BootstrapServers,
                AutoCommitIntervalMs = configuration.AutoCommitIntervalMs ?? AutoCommitIntervalMs,
                AutoOffsetReset = configuration.AutoOffsetReset ?? Messaging.Configuration.Kafka.AutoOffsetReset.Earliest
            },
            rebalanceListener);
    }

    public IKafkaProducer<byte[], byte[]> CreateProducer(KafkaProducerConfiguration configuration) =>
        _innerFactory.CreateProducer(configuration with { BootstrapServers = _containerHost.BootstrapServers });
}
