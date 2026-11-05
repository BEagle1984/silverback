// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

internal class ConfluentProducerWrapper : BrokerClient, IConfluentProducerWrapper
{
    private readonly IConfluentProducerBuilder _producerBuilder;

    private readonly ISilverbackLogger<ConfluentProducerWrapper> _logger;

    private readonly System.Threading.Lock _confluentProducerLock = new();

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private IProducer<byte[]?, byte[]?>? _confluentProducer;

    private bool _hasInitializedTransactions;

    public ConfluentProducerWrapper(
        string name,
        IConfluentProducerBuilder producerBuilder,
        KafkaProducerConfiguration configuration,
        IBrokerClientCallbacksInvoker brokerClientCallbacksInvoker,
        ISilverbackLogger<ConfluentProducerWrapper> logger)
        : base(name, logger)
    {
        Configuration = Check.NotNull(configuration, nameof(configuration));
        Check.NotNull(brokerClientCallbacksInvoker, nameof(brokerClientCallbacksInvoker));
        _logger = Check.NotNull(logger, nameof(logger));

        _producerBuilder = Check.NotNull(producerBuilder, nameof(producerBuilder))
            .SetConfiguration(configuration.ToConfluentConfig())
            .SetEventsHandlers(this, brokerClientCallbacksInvoker, Check.NotNull(logger, nameof(logger)));
    }

    public KafkaProducerConfiguration Configuration { get; }

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    public KafkaProducer Producer
    {
        get => field ?? throw new InvalidOperationException("The producer is not initialized yet.");
        set => field = Check.NotNull(value, nameof(value));
    }

    public void Produce(TopicPartition topicPartition, Message<byte[]?, byte[]?> message, Action<DeliveryReport<byte[]?, byte[]?>> deliveryHandler)
    {
        IProducer<byte[]?, byte[]?> confluentProducer = EnsureConnected();

        try
        {
            confluentProducer.Produce(topicPartition, message, deliveryHandler);
        }
        catch (KafkaException exception) when (exception.Error.IsFatal)
        {
            DisposeFailedConfluentProducer(confluentProducer);

            throw;
        }
    }

    public async Task<DeliveryResult<byte[]?, byte[]?>> ProduceAsync(
        TopicPartition topicPartition,
        Message<byte[]?, byte[]?> message,
        CancellationToken cancellationToken)
    {
        IProducer<byte[]?, byte[]?> confluentProducer = EnsureConnected();

        try
        {
            return await confluentProducer.ProduceAsync(topicPartition, message, cancellationToken).ConfigureAwait(false);
        }
        catch (KafkaException exception) when (exception.Error.IsFatal)
        {
            DisposeFailedConfluentProducer(confluentProducer);

            throw;
        }
    }

    public void InitTransactions()
    {
        IProducer<byte[]?, byte[]?> confluentProducer = EnsureConnected();

        if (_hasInitializedTransactions)
            return;

        try
        {
            confluentProducer.InitTransactions(Configuration.TransactionsInitTimeout);
            _logger.LogTransactionsInitialized(this);
            _hasInitializedTransactions = true;
        }
        catch (KafkaException exception) when (exception.Error.IsFatal)
        {
            DisposeFailedConfluentProducer(confluentProducer);

            throw;
        }
    }

    public void BeginTransaction()
    {
        IProducer<byte[]?, byte[]?> confluentProducer = EnsureConnected();

        try
        {
            confluentProducer.BeginTransaction();
            _hasInitializedTransactions = true;
            _logger.LogTransactionStarted(this);
        }
        catch (KafkaException exception) when (exception.Error.IsFatal)
        {
            DisposeFailedConfluentProducer(confluentProducer);

            throw;
        }
    }

    public void CommitTransaction()
    {
        IProducer<byte[]?, byte[]?> confluentProducer = EnsureConnected();

        try
        {
            confluentProducer.CommitTransaction(Configuration.TransactionCommitTimeout);
            _logger.LogTransactionCommitted(this);
        }
        catch (KafkaException exception) when (exception.Error.IsFatal)
        {
            DisposeFailedConfluentProducer(confluentProducer);

            throw;
        }
    }

    public void AbortTransaction()
    {
        IProducer<byte[]?, byte[]?> confluentProducer = EnsureConnected();

        try
        {
            confluentProducer.AbortTransaction(Configuration.TransactionAbortTimeout);
            _logger.LogTransactionAborted(this);
        }
        catch (KafkaException exception) when (exception.Error.IsFatal)
        {
            DisposeFailedConfluentProducer(confluentProducer);

            throw;
        }
    }

    public void SendOffsetsToTransaction(IReadOnlyCollection<TopicPartitionOffset> offsets, IConsumerGroupMetadata groupMetadata)
    {
        IProducer<byte[]?, byte[]?> confluentProducer = EnsureConnected();

        try
        {
            confluentProducer.SendOffsetsToTransaction(offsets, groupMetadata, Configuration.SendOffsetToTransactionTimeout);

            foreach (TopicPartitionOffset offset in offsets)
            {
                _logger.LogOffsetSentToTransaction(this, offset);
            }
        }
        catch (KafkaException exception) when (exception.Error.IsFatal)
        {
            DisposeFailedConfluentProducer(confluentProducer);

            throw;
        }
    }

    protected override ValueTask ConnectCoreAsync()
    {
        lock (_confluentProducerLock)
        {
            _confluentProducer = _producerBuilder.Build();
        }

        return default;
    }

    protected override ValueTask DisconnectCoreAsync()
    {
        IProducer<byte[]?, byte[]?>? confluentProducer;

        lock (_confluentProducerLock)
        {
            confluentProducer = _confluentProducer;
            _confluentProducer = null;
            _hasInitializedTransactions = false;
        }

        confluentProducer?.Flush();
        confluentProducer?.Dispose();

        return default;
    }

    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "The status can change concurrently")]
    private IProducer<byte[]?, byte[]?> EnsureConnected()
    {
        if (Status != ClientStatus.Initialized)
            throw new InvalidOperationException("The producer is not connected.");

        IProducer<byte[]?, byte[]?>? confluentProducer = Volatile.Read(ref _confluentProducer);

        if (confluentProducer != null)
            return confluentProducer;

        lock (_confluentProducerLock)
        {
            // Check again inside the lock, to avoid recreating the producer while disconnecting
            if (Status != ClientStatus.Initialized)
                throw new InvalidOperationException("The producer is not connected.");

            // Recreate the producer disposed after a fatal error
            return _confluentProducer ??= _producerBuilder.Build();
        }
    }

    private void DisposeFailedConfluentProducer(IProducer<byte[]?, byte[]?> confluentProducer)
    {
        lock (_confluentProducerLock)
        {
            // Another call already disposed this instance and possibly recreated the producer
            if (_confluentProducer != confluentProducer)
                return;

            _confluentProducer = null;
            _hasInitializedTransactions = false;
        }

        confluentProducer.Dispose();
    }
}
