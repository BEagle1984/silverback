// Copyright (c) 2025 Sergio Aquilini
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

    private bool _isPending;

    public KafkaTransaction(ISilverbackContext context, string? transactionalIdSuffix = null)
    {
        _context = Check.NotNull(context, nameof(context));
        TransactionalIdSuffix = transactionalIdSuffix;

        _context.AddKafkaTransaction(this);
    }

    public string? TransactionalIdSuffix { get; internal set; }

    public void Commit()
    {
        if (_confluentProducer == null)
            throw new InvalidOperationException("The transaction is not bound to a producer.");

        EnsureIsPending();

        if (_context.TryGetConsumerPipelineContext(out ConsumerPipelineContext? consumerPipelineContext) &&
            consumerPipelineContext is
            {
                Consumer: KafkaConsumer { Configuration.SendOffsetsToTransaction: true } kafkaConsumer
            })
        {
            _confluentProducer.SendOffsetsToTransaction(
                consumerPipelineContext.GetCommitIdentifiers().Cast<KafkaOffset>()
                    .Select(offset => new TopicPartitionOffset(offset.TopicPartition, offset.Offset + 1)) // Commit next offset (+1)
                    .ToArray(),
                kafkaConsumer.Client.GetConsumerGroupMetadata());
        }

        _confluentProducer.CommitTransaction();
        _isPending = false;
        _confluentProducer = null;
    }

    public void Abort()
    {
        EnsureIsPending();

        _confluentProducer?.AbortTransaction();
        _isPending = false;
        _confluentProducer = null;
    }

    public void AbortIfPending()
    {
        if (_isPending)
            Abort();
    }

    public void Dispose()
    {
        if (_isPending)
            Abort();

        _context.ClearKafkaTransaction();
    }

    internal void EnsureBegin()
    {
        if (_confluentProducer == null)
            throw new InvalidOperationException("The transaction is not bound to a producer.");

        if (!_isPending)
            Begin();
    }

    internal void Begin()
    {
        if (_confluentProducer == null)
            throw new InvalidOperationException("The transaction is not bound to a producer.");

        _confluentProducer.BeginTransaction();
        _isPending = true;
    }

    internal void BindConfluentProducer(IConfluentProducerWrapper confluentProducer)
    {
        if (_confluentProducer != null && confluentProducer != _confluentProducer)
            throw new InvalidOperationException("The transaction is already bound to a producer.");

        _confluentProducer = confluentProducer;
    }

    private void EnsureIsPending()
    {
        if (!_isPending)
            throw new InvalidOperationException("The transaction is not pending.");
    }
}
