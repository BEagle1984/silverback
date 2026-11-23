// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Confluent.Kafka;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Util;

namespace Silverback.Messaging.Transactions;

internal sealed class KafkaTransaction : IKafkaTransaction
{
    private readonly ISilverbackContext _context;

    private IConfluentProducerWrapper? _confluentProducer;

    // The bound producer, set only while the transaction is pending
    private IConfluentProducerWrapper? _pendingTransactionProducer;

    public KafkaTransaction(ISilverbackContext context, string? transactionalIdSuffix = null)
    {
        _context = Check.NotNull(context);
        TransactionalIdSuffix = transactionalIdSuffix;

        _context.AddKafkaTransaction(this);
    }

    public string? TransactionalIdSuffix { get; internal set; }

    public void Commit()
    {
        if (_confluentProducer == null)
            throw new InvalidOperationException("The transaction is not bound to a producer.");

        IConfluentProducerWrapper confluentProducer = GetPendingTransactionProducer();

        if (_context.TryGetConsumerPipelineContext(out ConsumerPipelineContext? consumerPipelineContext) &&
            consumerPipelineContext is
            {
                Consumer: KafkaConsumer { Configuration.SendOffsetsToTransaction: true } kafkaConsumer
            })
        {
            confluentProducer.SendOffsetsToTransaction(
                [.. consumerPipelineContext.GetCommitIdentifiers().Cast<KafkaOffset>().Select(offset => new TopicPartitionOffset(offset.TopicPartition, offset.Offset + 1))],
                kafkaConsumer.Client.GetConsumerGroupMetadata());
        }

        confluentProducer.CommitTransaction();
        _pendingTransactionProducer = null;
        _confluentProducer = null;
    }

    public void Abort()
    {
        IConfluentProducerWrapper confluentProducer = GetPendingTransactionProducer();

        confluentProducer.AbortTransaction();
        _pendingTransactionProducer = null;
        _confluentProducer = null;
    }

    public void AbortIfPending()
    {
        if (_pendingTransactionProducer != null)
            Abort();
    }

    public void Dispose()
    {
        if (_pendingTransactionProducer != null)
            Abort();

        _context.ClearKafkaTransaction();
    }

    internal void EnsureBegin()
    {
        if (_confluentProducer == null)
            throw new InvalidOperationException("The transaction is not bound to a producer.");

        if (_pendingTransactionProducer == null)
            Begin();
    }

    internal void Begin()
    {
        if (_confluentProducer == null)
            throw new InvalidOperationException("The transaction is not bound to a producer.");

        _confluentProducer.BeginTransaction();
        _pendingTransactionProducer = _confluentProducer;
    }

    internal void BindConfluentProducer(IConfluentProducerWrapper confluentProducer)
    {
        if (_confluentProducer != null && confluentProducer != _confluentProducer)
            throw new InvalidOperationException("The transaction is already bound to a producer.");

        _confluentProducer = confluentProducer;
    }

    private IConfluentProducerWrapper GetPendingTransactionProducer() =>
        _pendingTransactionProducer ?? throw new InvalidOperationException("The transaction is not pending.");
}
