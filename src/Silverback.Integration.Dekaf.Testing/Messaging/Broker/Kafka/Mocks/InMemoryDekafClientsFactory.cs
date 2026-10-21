// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Dekaf.Consumer;
using Dekaf.Producer;
using Dekaf.Testing;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;
using DekafAutoOffsetReset = Dekaf.Consumer.AutoOffsetReset;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

/// <summary>
///     Creates the Dekaf.Testing in-memory clients, all connected to the same <see cref="InMemoryKafkaCluster" />.
/// </summary>
internal sealed class InMemoryDekafClientsFactory : IDekafClientsFactory
{
    private readonly ConcurrentQueue<TrackingInMemoryConsumer> _consumers = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryDekafClientsFactory" /> class.
    /// </summary>
    /// <param name="cluster">
    ///     The in-memory cluster the clients connect to.
    /// </param>
    public InMemoryDekafClientsFactory(InMemoryKafkaCluster cluster)
    {
        Cluster = Check.NotNull(cluster, nameof(cluster));
    }

    /// <summary>
    ///     Gets the in-memory cluster the clients connect to.
    /// </summary>
    public InMemoryKafkaCluster Cluster { get; }

    internal IReadOnlyCollection<TrackingInMemoryConsumer> Consumers => _consumers;

    /// <inheritdoc cref="IDekafClientsFactory.CreateConsumer" />
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by the TrackingInMemoryConsumer")]
    public IKafkaConsumer<byte[], byte[]> CreateConsumer(KafkaConsumerConfiguration configuration, IConsumerAwareRebalanceListener rebalanceListener)
    {
        Check.NotNull(configuration, nameof(configuration));

        DekafAutoOffsetReset autoOffsetReset = configuration.AutoOffsetReset.HasValue
            ? DekafClientsFactory.MapAutoOffsetReset(configuration.AutoOffsetReset.Value)
            : DekafAutoOffsetReset.Latest;

        InMemoryConsumer<byte[], byte[]> innerConsumer = new(
            Cluster,
            new InMemoryConsumerOptions
            {
                GroupId = configuration.GroupId,
                GroupInstanceId = configuration.GroupInstanceId,
                AutoOffsetReset = autoOffsetReset,

                // The auto-commit is emulated by the TrackingInMemoryConsumer
                OffsetCommitMode = OffsetCommitMode.Manual,
                EnableAutoOffsetStore = DekafClientsFactory.EnableAutoOffsetStore,
                IsolationLevel = DekafClientsFactory.ConsumerIsolationLevel
            });

        TrackingInMemoryConsumer consumer = new(
            innerConsumer,
            rebalanceListener,
            configuration.GroupId,
            configuration.EnableAutoCommit);
        _consumers.Enqueue(consumer);
        return consumer;
    }

    /// <inheritdoc cref="IDekafClientsFactory.CreateProducer" />
    public IKafkaProducer<byte[], byte[]> CreateProducer(KafkaProducerConfiguration configuration)
    {
        Check.NotNull(configuration, nameof(configuration));

        // The in-memory producer supports transactions without requiring a transactional id
        return new InMemoryProducer<byte[], byte[]>(Cluster);
    }
}
