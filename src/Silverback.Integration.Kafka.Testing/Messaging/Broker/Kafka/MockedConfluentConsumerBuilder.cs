// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     The builder for the <see cref="MockedConfluentConsumer" />.
/// </summary>
public class MockedConfluentConsumerBuilder : IConfluentConsumerBuilder
{
    private readonly IInMemoryTopicCollection _topics;

    private readonly IMockedConsumerGroupsCollection _consumerGroups;

    private readonly IMockedKafkaOptions _options;

    private ConsumerConfig? _config;

    private Action<IConsumer<byte[]?, byte[]?>, string>? _statisticsHandler;

    private Action<IConsumer<byte[]?, byte[]?>, Error>? _errorHandler;

    private Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>?
        _partitionsAssignedHandler;

    private Func<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>>?
        _partitionsRevokedHandler;

    private Action<IConsumer<byte[]?, byte[]?>, CommittedOffsets>? _offsetsCommittedHandler;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MockedConfluentConsumerBuilder" /> class.
    /// </summary>
    /// <param name="topics">
    ///     The <see cref="IInMemoryTopicCollection" />.
    /// </param>
    /// <param name="consumerGroups">
    ///     The <see cref="IMockedConsumerGroupsCollection" />.
    /// </param>
    /// <param name="options">
    ///     The <see cref="IMockedKafkaOptions" />.
    /// </param>
    public MockedConfluentConsumerBuilder(
        IInMemoryTopicCollection topics,
        IMockedConsumerGroupsCollection consumerGroups,
        IMockedKafkaOptions options)
    {
        _topics = Check.NotNull(topics, nameof(topics));
        _consumerGroups = Check.NotNull(consumerGroups, nameof(consumerGroups));
        _options = Check.NotNull(options, nameof(options));
    }

    /// <inheritdoc cref="IConfluentConsumerBuilder.SetConfig" />
    public IConfluentConsumerBuilder SetConfig(ConsumerConfig config)
    {
        _config = config;
        return this;
    }

    /// <inheritdoc cref="IConfluentConsumerBuilder.SetStatisticsHandler" />
    public IConfluentConsumerBuilder SetStatisticsHandler(Action<IConsumer<byte[]?, byte[]?>, string> statisticsHandler)
    {
        _statisticsHandler = statisticsHandler;
        return this;
    }

    /// <inheritdoc cref="IConfluentConsumerBuilder.SetErrorHandler" />
    public IConfluentConsumerBuilder SetErrorHandler(Action<IConsumer<byte[]?, byte[]?>, Error> errorHandler)
    {
        _errorHandler = errorHandler;
        return this;
    }

    /// <inheritdoc cref="IConfluentConsumerBuilder.SetPartitionsAssignedHandler(Func{IConsumer{byte[],byte[]},List{TopicPartition},IEnumerable{TopicPartitionOffset}})" />
    public IConfluentConsumerBuilder SetPartitionsAssignedHandler(
        Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>
            partitionsAssignedHandler)
    {
        _partitionsAssignedHandler = partitionsAssignedHandler;
        return this;
    }

    /// <inheritdoc cref="IConfluentConsumerBuilder.SetPartitionsAssignedHandler(Action{IConsumer{byte[],byte[]},List{TopicPartition}})" />
    public IConfluentConsumerBuilder SetPartitionsAssignedHandler(Action<IConsumer<byte[]?, byte[]?>, List<TopicPartition>> partitionsAssignedHandler)
    {
        _partitionsAssignedHandler = (consumer, partitions) =>
        {
            partitionsAssignedHandler(consumer, partitions);

            return partitions
                .Select(topicPartition => new TopicPartitionOffset(topicPartition, Offset.Unset))
                .ToList();
        };

        return this;
    }

    /// <inheritdoc cref="IConfluentConsumerBuilder.SetPartitionsRevokedHandler(Func{IConsumer{byte[],byte[]},List{TopicPartitionOffset},IEnumerable{TopicPartitionOffset}})" />
    public IConfluentConsumerBuilder SetPartitionsRevokedHandler(
        Func<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>>
            partitionsRevokedHandler)
    {
        _partitionsRevokedHandler = partitionsRevokedHandler;
        return this;
    }

    /// <inheritdoc cref="IConfluentConsumerBuilder.SetPartitionsRevokedHandler(Action{IConsumer{byte[],byte[]},List{TopicPartitionOffset}})" />
    public IConfluentConsumerBuilder SetPartitionsRevokedHandler(Action<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>> partitionsRevokedHandler)
    {
        _partitionsRevokedHandler = (consumer, partitions) =>
        {
            partitionsRevokedHandler(consumer, partitions);

            return Enumerable.Empty<TopicPartitionOffset>();
        };

        return this;
    }

    /// <inheritdoc cref="IConfluentConsumerBuilder.SetOffsetsCommittedHandler" />
    public IConfluentConsumerBuilder SetOffsetsCommittedHandler(Action<IConsumer<byte[]?, byte[]?>, CommittedOffsets> offsetsCommittedHandler)
    {
        _offsetsCommittedHandler = offsetsCommittedHandler;
        return this;
    }

    /// <inheritdoc cref="IConfluentConsumerBuilder.SetLogHandler" />
    // Not yet implemented / not needed
    public IConfluentConsumerBuilder SetLogHandler(Action<IConsumer<byte[]?, byte[]?>, LogMessage> logHandler) => this;

    /// <inheritdoc cref="IConfluentConsumerBuilder.Build" />
    public IConsumer<byte[]?, byte[]?> Build()
    {
        if (_config == null)
            throw new InvalidOperationException("SetConfig must be called to provide the consumer configuration.");

        return new MockedConfluentConsumer(_config, _topics, _consumerGroups, _options)
        {
            StatisticsHandler = _statisticsHandler,
            ErrorHandler = _errorHandler,
            PartitionsAssignedHandler = _partitionsAssignedHandler,
            PartitionsRevokedHandler = _partitionsRevokedHandler,
            OffsetsCommittedHandler = _offsetsCommittedHandler
        };
    }
}
