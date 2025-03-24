// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

[SuppressMessage("", "CA1812", Justification = "Class used via DI")]
internal class InMemoryTransactionManager : IInMemoryTransactionManager
{
    private readonly IInMemoryTopicCollection _topics;

    private readonly List<TransactionalProducerInfo> _producersInfo = [];

    public InMemoryTransactionManager(IInMemoryTopicCollection topics)
    {
        _topics = Check.NotNull(topics, nameof(topics));
    }

    public Guid InitTransaction(string transactionalId)
    {
        Check.NotNull(transactionalId, nameof(transactionalId));

        lock (_producersInfo)
        {
            TransactionalProducerInfo? producerInfo = _producersInfo.Find(info => info.TransactionalId == transactionalId);

            if (producerInfo == null)
            {
                producerInfo = new TransactionalProducerInfo(transactionalId);
                _producersInfo.Add(producerInfo);
            }
            else
            {
                _topics.ForEach(topic => topic.AbortTransaction(producerInfo.TransactionalUniqueId));
                producerInfo.Reset();
            }

            return producerInfo.TransactionalUniqueId;
        }
    }

    public void BeginTransaction(Guid transactionalUniqueId)
    {
        lock (_producersInfo)
        {
            TransactionalProducerInfo producerInfo = GetProducerInfo(transactionalUniqueId);
            producerInfo.IsTransactionPending = true;
        }
    }

    public void CommitTransaction(Guid transactionalUniqueId)
    {
        lock (_producersInfo)
        {
            TransactionalProducerInfo producerInfo = GetProducerInfo(transactionalUniqueId);

            if (!producerInfo.IsTransactionPending)
                throw new InvalidOperationException("No pending transaction.");

            _topics.ForEach(topic => topic.CommitTransaction(producerInfo.TransactionalUniqueId));

            foreach (GroupPendingOffsets pendingOffsets in producerInfo.PendingOffsets)
            {
                pendingOffsets.GroupMetadata.ConsumerGroup.Commit(pendingOffsets.Offsets);
            }

            producerInfo.PendingOffsets.Clear();
        }
    }

    public void AbortTransaction(Guid transactionalUniqueId)
    {
        lock (_producersInfo)
        {
            TransactionalProducerInfo producerInfo = GetProducerInfo(transactionalUniqueId);

            if (!producerInfo.IsTransactionPending)
                throw new InvalidOperationException("No pending transaction.");

            _topics.ForEach(topic => topic.AbortTransaction(producerInfo.TransactionalUniqueId));
            producerInfo.PendingOffsets.Clear();
        }
    }

    public void SendOffsetsToTransaction(Guid transactionalUniqueId, IEnumerable<TopicPartitionOffset> offsets, IConsumerGroupMetadata groupMetadata)
    {
        lock (_producersInfo)
        {
            TransactionalProducerInfo producerInfo = GetProducerInfo(transactionalUniqueId);

            if (!producerInfo.IsTransactionPending)
                throw new InvalidOperationException("No pending transaction.");

            producerInfo.PendingOffsets.Add(new GroupPendingOffsets(offsets, (MockedConsumerGroupMetadata)groupMetadata));
        }
    }

    public bool IsTransactionPending(Guid transactionalUniqueId)
    {
        if (transactionalUniqueId == Guid.Empty)
            return false;

        lock (_producersInfo)
        {
            TransactionalProducerInfo producerInfo = GetProducerInfo(transactionalUniqueId);
            return producerInfo.IsTransactionPending;
        }
    }

    private TransactionalProducerInfo GetProducerInfo(Guid transactionalUniqueId)
    {
        if (transactionalUniqueId == Guid.Empty)
            throw new InvalidOperationException("Transactions have not been initialized.");

        return _producersInfo.Find(info => info.TransactionalUniqueId == transactionalUniqueId) ??
               throw new InvalidOperationException("The producer has been fenced.");
    }

    private sealed class TransactionalProducerInfo
    {
        public TransactionalProducerInfo(string transactionalId)
        {
            TransactionalId = transactionalId;
        }

        public string TransactionalId { get; }

        public Guid TransactionalUniqueId { get; private set; } = Guid.NewGuid();

        public bool IsTransactionPending { get; set; }

        public List<GroupPendingOffsets> PendingOffsets { get; } = [];

        public void Reset()
        {
            IsTransactionPending = false;
            PendingOffsets.Clear();
            TransactionalUniqueId = Guid.NewGuid();
        }
    }

    private sealed record GroupPendingOffsets(IEnumerable<TopicPartitionOffset> Offsets, MockedConsumerGroupMetadata GroupMetadata);
}
