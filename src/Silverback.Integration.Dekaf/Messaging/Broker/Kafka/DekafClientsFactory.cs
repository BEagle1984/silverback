// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Dekaf.Consumer;
using Dekaf.Producer;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;
using ConsumerBuilder = Dekaf.ConsumerBuilder<byte[], byte[]>;
using DekafAcks = Dekaf.Producer.Acks;
using DekafAutoOffsetReset = Dekaf.Consumer.AutoOffsetReset;
using DekafKafka = Dekaf.Kafka;
using IsolationLevel = Dekaf.Protocol.Messages.IsolationLevel;
using ProducerBuilder = Dekaf.ProducerBuilder<byte[], byte[]>;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Builds the real Dekaf clients, mapping the Silverback configuration onto the Dekaf builders.
/// </summary>
internal sealed class DekafClientsFactory : IDekafClientsFactory
{
    // Silverback stores the offsets explicitly after the message has been successfully processed and reads only the
    // committed transactional messages, like the librdkafka default. Applied by the in-memory clients factory as well.
    internal const bool EnableAutoOffsetStore = false;

    internal const IsolationLevel ConsumerIsolationLevel = IsolationLevel.ReadCommitted;

    private readonly ILoggerFactory? _loggerFactory;

    private readonly IPartitioner? _partitioner;

    public DekafClientsFactory(ILoggerFactory? loggerFactory = null)
        : this(loggerFactory, null)
    {
    }

    // The partitioner can only be overridden by the tests, which need a deterministic distribution of the keyless
    // messages across the partitions
    internal DekafClientsFactory(ILoggerFactory? loggerFactory, IPartitioner? partitioner)
    {
        _loggerFactory = loggerFactory;
        _partitioner = partitioner;
    }

    public IKafkaConsumer<byte[], byte[]> CreateConsumer(KafkaConsumerConfiguration configuration, IConsumerAwareRebalanceListener rebalanceListener)
    {
        Check.NotNull(configuration, nameof(configuration));
        Check.NotNull(rebalanceListener, nameof(rebalanceListener));

        ConsumerBuilder builder = DekafKafka.CreateConsumer<byte[], byte[]>()
            .WithBootstrapServers(configuration.BootstrapServers!)
            .WithRebalanceListener(rebalanceListener)
            .WithIsolationLevel(ConsumerIsolationLevel)
            .WithAutoOffsetStore(EnableAutoOffsetStore)
            .WithOffsetCommitMode(UsesDekafAutoCommit(configuration) ? OffsetCommitMode.Auto : OffsetCommitMode.Manual);

        if (!string.IsNullOrEmpty(configuration.ClientId))
            builder.WithClientId(configuration.ClientId);

        if (!string.IsNullOrEmpty(configuration.GroupId))
            builder.WithGroupId(configuration.GroupId);

        if (!string.IsNullOrEmpty(configuration.GroupInstanceId))
            builder.WithGroupInstanceId(configuration.GroupInstanceId);

        if (configuration.AutoOffsetReset.HasValue)
            builder.WithAutoOffsetReset(MapAutoOffsetReset(configuration.AutoOffsetReset.Value));

        if (configuration.AutoCommitIntervalMs.HasValue)
            builder.WithAutoCommitInterval(TimeSpan.FromMilliseconds(configuration.AutoCommitIntervalMs.Value));

        if (configuration.EnablePartitionEof.HasValue)
            builder.WithPartitionEof(configuration.EnablePartitionEof.Value);

        if (_loggerFactory != null)
            builder.WithLoggerFactory(_loggerFactory);

        return builder.Build();
    }

    public IKafkaProducer<byte[], byte[]> CreateProducer(KafkaProducerConfiguration configuration)
    {
        Check.NotNull(configuration, nameof(configuration));

        ProducerBuilder builder = DekafKafka.CreateProducer<byte[], byte[]>()
            .WithBootstrapServers(configuration.BootstrapServers!);

        // By default keep the librdkafka partitioner, so that the same key lands on the same partition as with Confluent.Kafka
        if (_partitioner != null)
            builder.WithCustomPartitioner(_partitioner);
        else
            builder.WithPartitioner(PartitionerType.ConsistentRandom);

        if (!string.IsNullOrEmpty(configuration.ClientId))
            builder.WithClientId(configuration.ClientId);

        if (configuration.Acks.HasValue)
            builder.WithAcks(MapAcks(configuration.Acks.Value));

        // Dekaf enables the idempotence by default, which requires acks=all: unless explicitly enabled, it's disabled
        // when fewer acks are requested, as librdkafka (where it's disabled by default) would allow
        if (configuration.EnableIdempotence.HasValue)
            builder.WithIdempotence(configuration.EnableIdempotence.Value);
        else if (configuration.Acks is not null and not Configuration.Kafka.Acks.All && !configuration.IsTransactional)
            builder.WithIdempotence(false);

        if (configuration.LingerMs.HasValue)
            builder.WithLinger(TimeSpan.FromMilliseconds(configuration.LingerMs.Value));

        // Silverback's transaction API is synchronous: the produce continuations must not run inline on the broker
        // sender thread, which would then be blocked by the commit or abort waited for synchronously
        if (!string.IsNullOrEmpty(configuration.TransactionalId))
        {
            builder
                .WithTransactionalId(configuration.TransactionalId)
                .WithInlineTransactionCompletions(false);
        }

        if (_loggerFactory != null)
            builder.WithLoggerFactory(_loggerFactory);

        return builder.Build();
    }

    // Dekaf only auto-commits while the consumer is a joined group member, which a static assignment never is: the
    // DekafConsumerWrapper commits periodically in that case. Applied by the in-memory clients factory as well.
    internal static bool UsesDekafAutoCommit(KafkaConsumerConfiguration configuration) =>
        configuration.EnableAutoCommit && !configuration.IsStaticAssignment;

    internal static DekafAutoOffsetReset MapAutoOffsetReset(Configuration.Kafka.AutoOffsetReset autoOffsetReset) =>
        autoOffsetReset switch
        {
            Configuration.Kafka.AutoOffsetReset.Earliest => DekafAutoOffsetReset.Earliest,
            Configuration.Kafka.AutoOffsetReset.Latest => DekafAutoOffsetReset.Latest,
            Configuration.Kafka.AutoOffsetReset.Error => DekafAutoOffsetReset.None,
            _ => throw new ArgumentOutOfRangeException(nameof(autoOffsetReset), autoOffsetReset, null)
        };

    private static DekafAcks MapAcks(Configuration.Kafka.Acks acks) =>
        acks switch
        {
            Configuration.Kafka.Acks.None => DekafAcks.None,
            Configuration.Kafka.Acks.Leader => DekafAcks.Leader,
            Configuration.Kafka.Acks.All => DekafAcks.All,
            _ => throw new ArgumentOutOfRangeException(nameof(acks), acks, null)
        };
}
