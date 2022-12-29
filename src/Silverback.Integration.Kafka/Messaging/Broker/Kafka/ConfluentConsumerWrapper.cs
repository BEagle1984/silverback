// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

// TODO: Test (above all assignment, subscription, etc.)
internal class ConfluentConsumerWrapper : BrokerClient, IConfluentConsumerWrapper
{
    private readonly KafkaConsumerConfiguration _configuration;

    private readonly IBrokerClientCallbacksInvoker _brokerClientCallbacksInvoker;

    private readonly IKafkaOffsetStoreFactory _offsetStoreFactory;

    private readonly ISilverbackLogger _logger;

    private readonly IConfluentConsumerBuilder _consumerBuilder;

    private readonly IConfluentAdminClientBuilder _adminClientBuilder;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private IConsumer<byte[]?, byte[]?>? _confluentConsumer;

    private IAdminClient? _adminClient;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private KafkaConsumer? _consumer;

    public ConfluentConsumerWrapper(
        string name,
        IConfluentConsumerBuilder consumerBuilder,
        KafkaConsumerConfiguration configuration,
        IConfluentAdminClientBuilder adminClientBuilder,
        IBrokerClientCallbacksInvoker brokerClientCallbacksInvoker,
        IKafkaOffsetStoreFactory offsetStoreFactory,
        ISilverbackLogger logger)
        : base(name, logger)
    {
        _configuration = Check.NotNull(configuration, nameof(configuration));
        _brokerClientCallbacksInvoker = Check.NotNull(brokerClientCallbacksInvoker, nameof(brokerClientCallbacksInvoker));
        _offsetStoreFactory = Check.NotNull(offsetStoreFactory, nameof(offsetStoreFactory));
        _logger = Check.NotNull(logger, nameof(logger));

        _consumerBuilder = Check.NotNull(consumerBuilder, nameof(consumerBuilder))
            .SetConfig(configuration.GetConfluentClientConfig())
            .SetEventsHandlers(this, _configuration, _brokerClientCallbacksInvoker, _logger);
        _adminClientBuilder = Check.NotNull(adminClientBuilder, nameof(adminClientBuilder));
    }

    public IReadOnlyList<TopicPartition> Assignment =>
        (IReadOnlyList<TopicPartition>?)_confluentConsumer?.Assignment ?? Array.Empty<TopicPartition>();

    public KafkaConsumer Consumer
    {
        get => _consumer ?? throw new InvalidOperationException("The consumer is not initialized yet.");
        set => _consumer = Check.NotNull(value, nameof(value));
    }

    public ConsumeResult<byte[]?, byte[]?> Consume(CancellationToken cancellationToken)
    {
        if (Status is not (ClientStatus.Initialized or ClientStatus.Initializing))
            throw new InvalidOperationException("The consumer is not connected.");

        if (_confluentConsumer == null)
            throw new InvalidOperationException("The underlying consumer is not initialized.");

        return _confluentConsumer.Consume(cancellationToken);
    }

    public void StoreOffset(TopicPartitionOffset topicPartitionOffset)
    {
        if (Status is not (ClientStatus.Initialized or ClientStatus.Disconnecting))
            throw new InvalidOperationException("The consumer is not connected.");

        if (_confluentConsumer == null)
            throw new InvalidOperationException("The underlying consumer is not initialized.");

        if (_configuration.CommitOffsets)
            _confluentConsumer.StoreOffset(topicPartitionOffset);
    }

    public void Commit()
    {
        if (Status is not ClientStatus.Initialized and not ClientStatus.Disconnecting)
            throw new InvalidOperationException("The consumer is not connected.");

        if (_confluentConsumer == null)
            throw new InvalidOperationException("The underlying consumer is not initialized.");

        if (!_configuration.CommitOffsets)
            return;

        try
        {
            List<TopicPartitionOffset>? offsets = _confluentConsumer.Commit();

            CommittedOffsets committedOffsets = new(
                offsets.Select(offset => new TopicPartitionOffsetError(offset, new Error(ErrorCode.NoError))).ToList(),
                new Error(ErrorCode.NoError));

            _brokerClientCallbacksInvoker.Invoke<IKafkaOffsetCommittedCallback>(
                callback =>
                    callback.OnOffsetsCommitted(committedOffsets, Consumer));
        }
        catch (TopicPartitionOffsetException ex)
        {
            _brokerClientCallbacksInvoker.Invoke<IKafkaOffsetCommittedCallback>(
                callback =>
                    callback.OnOffsetsCommitted(new CommittedOffsets(ex.Results, ex.Error), Consumer));

            throw;
        }
        catch (KafkaException ex)
        {
            _brokerClientCallbacksInvoker.Invoke<IKafkaOffsetCommittedCallback>(
                callback =>
                    callback.OnOffsetsCommitted(new CommittedOffsets(null, ex.Error), Consumer));
        }
    }

    public void Seek(TopicPartitionOffset topicPartitionOffset)
    {
        if (Status != ClientStatus.Initialized)
            throw new InvalidOperationException("The consumer is not connected.");

        if (_confluentConsumer == null)
            throw new InvalidOperationException("The underlying consumer is not initialized.");

        _confluentConsumer.Seek(topicPartitionOffset);
    }

    public void Pause(IEnumerable<TopicPartition> partitions)
    {
        if (Status != ClientStatus.Initialized)
            throw new InvalidOperationException("The consumer is not connected.");

        if (_confluentConsumer == null)
            throw new InvalidOperationException("The underlying consumer is not initialized.");

        _confluentConsumer.Pause(partitions);
    }

    public void Resume(IEnumerable<TopicPartition> partitions)
    {
        if (Status != ClientStatus.Initialized)
            throw new InvalidOperationException("The consumer is not connected.");

        if (_confluentConsumer == null)
            throw new InvalidOperationException("The underlying consumer is not initialized.");

        _confluentConsumer.Resume(partitions);
    }

    protected override async ValueTask ConnectCoreAsync()
    {
        _confluentConsumer = _consumerBuilder.Build();

        if (_configuration.IsStaticAssignment)
            await PerformStaticAssignmentAsync().ConfigureAwait(false);
        else
            Subscribe();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    protected override ValueTask DisconnectCoreAsync()
    {
        if (!_configuration.EnableAutoCommit)
            Commit();

        _confluentConsumer?.Close();
        _confluentConsumer?.Dispose();
        _confluentConsumer = null;

        return default;
    }

    private async ValueTask PerformStaticAssignmentAsync()
    {
        List<TopicPartitionOffset> assignment = new();

        try
        {
            foreach (KafkaConsumerEndpointConfiguration endpointConfiguration in _configuration.Endpoints)
            {
                await foreach (TopicPartitionOffset topicPartitionOffset in GetAssignmentAsync(endpointConfiguration))
                {
                    assignment.Add(topicPartitionOffset);
                }
            }
        }
        finally
        {
            _adminClient?.Dispose();
            _adminClient = null;
        }

        _confluentConsumer!.Assign(assignment);

        assignment.ForEach(topicPartitionOffset => _logger.LogPartitionStaticallyAssigned(topicPartitionOffset, Consumer));
    }

    private async IAsyncEnumerable<TopicPartitionOffset> GetAssignmentAsync(KafkaConsumerEndpointConfiguration endpointConfiguration)
    {
        StoredOffsetsLoader storedOffsetsLoader = new(_offsetStoreFactory, _configuration);

        foreach (TopicPartitionOffset topicPartitionOffset in endpointConfiguration.TopicPartitions)
        {
            if (topicPartitionOffset.Partition == Partition.Any && endpointConfiguration.PartitionOffsetsProvider != null)
            {
                IEnumerable<TopicPartitionOffset> providedTopicPartitionOffsets =
                    await GetAssignmentViaProviderAsync(endpointConfiguration.PartitionOffsetsProvider, topicPartitionOffset).ConfigureAwait(false);

                foreach (TopicPartitionOffset providedTopicPartitionOffset in providedTopicPartitionOffsets)
                {
                    yield return storedOffsetsLoader.ApplyStoredOffset(providedTopicPartitionOffset);
                }
            }
            else
            {
                yield return storedOffsetsLoader.ApplyStoredOffset(topicPartitionOffset);
            }
        }
    }

    private async ValueTask<IEnumerable<TopicPartitionOffset>> GetAssignmentViaProviderAsync(
        Func<IReadOnlyCollection<TopicPartition>, ValueTask<IEnumerable<TopicPartitionOffset>>> partitionOffsetsProvider,
        TopicPartitionOffset topicPartitionOffset)
    {
        _adminClient ??= _adminClientBuilder.Build(_configuration.GetConfluentClientConfig());

        List<TopicPartition> availablePartitions =
            _adminClient
                .GetMetadata(topicPartitionOffset.Topic, TimeSpan.FromMinutes(5)) // TODO: 5 minutes timeout?
                .Topics[0].Partitions
                .Select(metadata => new TopicPartition(topicPartitionOffset.Topic, metadata.PartitionId))
                .ToList();

        return await partitionOffsetsProvider.Invoke(availablePartitions).ConfigureAwait(false);
    }

    private void Subscribe() =>
        _confluentConsumer!.Subscribe(
            _configuration.Endpoints
                .SelectMany(endpoint => endpoint.TopicPartitions)
                .Select(topicPartitionOffset => topicPartitionOffset.Topic)
                .Distinct());
}
