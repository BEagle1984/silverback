// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private IProducer<byte[]?, byte[]?>? _confluentProducer;

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

    public void Produce(TopicPartition topicPartition, Message<byte[]?, byte[]?> message, Action<DeliveryReport<byte[]?, byte[]?>> deliveryHandler)
    {
        IProducer<byte[]?, byte[]?> confluentProducer = EnsureConnected();

        try
        {
            confluentProducer.Produce(topicPartition, message, deliveryHandler);
        }
        catch (KafkaException)
        {
            if (Configuration.DisposeOnException)
                DisposeConfluentProducer();

            throw;
        }
    }

    public Task<DeliveryResult<byte[]?, byte[]?>> ProduceAsync(TopicPartition topicPartition, Message<byte[]?, byte[]?> message)
    {
        IProducer<byte[]?, byte[]?> confluentProducer = EnsureConnected();

        try
        {
            return confluentProducer.ProduceAsync(topicPartition, message);
        }
        catch (KafkaException)
        {
            if (Configuration.DisposeOnException)
                DisposeConfluentProducer();

            throw;
        }
    }

    public void InitTransactions()
    {
        IProducer<byte[]?, byte[]?> confluentProducer = EnsureConnected();

        try
        {
            confluentProducer.InitTransactions(Configuration.TransactionsInitTimeout);
            _logger.LogTransactionsInitialized(this);
        }
        catch (KafkaException)
        {
            if (Configuration.DisposeOnException)
                DisposeConfluentProducer();

            throw;
        }
    }

    public void BeginTransaction()
    {
        IProducer<byte[]?, byte[]?> confluentProducer = EnsureConnected();

        try
        {
            confluentProducer.BeginTransaction();
            _logger.LogTransactionBegan(this);
        }
        catch (KafkaException)
        {
            if (Configuration.DisposeOnException)
                DisposeConfluentProducer();

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
        catch (KafkaException)
        {
            if (Configuration.DisposeOnException)
                DisposeConfluentProducer();

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
        catch (KafkaException)
        {
            if (Configuration.DisposeOnException)
                DisposeConfluentProducer();

            throw;
        }
    }

    public void SendOffsetsToTransaction(IReadOnlyCollection<TopicPartitionOffset> offsets, IConsumerGroupMetadata groupMetadata)
    {
        IProducer<byte[]?, byte[]?> confluentProducer = EnsureConnected();

        try
        {
            confluentProducer.SendOffsetsToTransaction(offsets, groupMetadata, TimeSpan.Zero); // TODO: Timespan from config

            foreach (TopicPartitionOffset offset in offsets)
            {
                _logger.LogOffsetSentToTransaction(this, offset);
            }
        }
        catch (KafkaException)
        {
            if (Configuration.DisposeOnException)
                DisposeConfluentProducer();

            throw;
        }
    }

    protected override ValueTask ConnectCoreAsync()
    {
        _confluentProducer = _producerBuilder.Build();
        return default;
    }

    protected override ValueTask DisconnectCoreAsync()
    {
        _confluentProducer?.Flush();
        DisposeConfluentProducer();

        return default;
    }

    private IProducer<byte[]?, byte[]?> EnsureConnected()
    {
        if (Status != ClientStatus.Initialized)
            throw new InvalidOperationException("The producer is not connected.");

        if (_confluentProducer == null)
            throw new InvalidOperationException("The underlying producer is not initialized.");

        return _confluentProducer;
    }

    private void DisposeConfluentProducer()
    {
        _confluentProducer?.Dispose();
        _confluentProducer = null;
    }
}
