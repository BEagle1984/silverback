// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka.Mocks
{
    internal sealed class InMemoryPartition : IInMemoryPartition
    {
        private readonly List<StoredMessage> _messages = new();

        private readonly List<Transaction> _transactions = new();

        public InMemoryPartition(in int index, InMemoryTopic topic)
        {
            Partition = new Partition(index);
            Topic = topic;
        }

        private enum StoredMessageStatus
        {
            Pending,

            Committed,

            Deleted
        }

        public Partition Partition { get; }

        public InMemoryTopic Topic { get; }

        public Offset FirstOffset { get; private set; } = Offset.Unset;

        public Offset LastOffset { get; private set; } = Offset.Unset;

        public int TotalMessagesCount { get; private set; }

        public Offset Add(Message<byte[]?, byte[]?> message, Guid transactionalUniqueId)
        {
            lock (_messages)
            {
                if (_messages.Count == 0)
                    LastOffset = FirstOffset = new Offset(0);
                else
                    LastOffset++;

                if (transactionalUniqueId == Guid.Empty)
                {
                    _messages.Add(new StoredMessage(message, LastOffset, StoredMessageStatus.Committed));
                    TotalMessagesCount++;
                }
                else
                {
                    Transaction? transaction = _transactions.FirstOrDefault(transaction => transaction.Id == transactionalUniqueId);

                    if (transaction == null)
                    {
                        transaction = new Transaction(transactionalUniqueId);
                        _transactions.Add(transaction);
                    }

                    StoredMessage storedMessage = new(message, LastOffset, StoredMessageStatus.Pending);
                    _messages.Add(storedMessage);
                    transaction.Messages.Add(storedMessage);
                }

                return LastOffset;
            }
        }

        public bool TryPull(Offset offset, out ConsumeResult<byte[]?, byte[]?>? result)
        {
            lock (_messages)
            {
                while (true)
                {
                    if (offset.Value > LastOffset || _messages.Count == 0)
                    {
                        result = null;
                        return false;
                    }

                    StoredMessage storedMessage = _messages[(int)(offset.Value - Math.Max(0, FirstOffset.Value))];

                    switch (storedMessage.Status)
                    {
                        case StoredMessageStatus.Pending:
                            result = null;
                            return false;
                        case StoredMessageStatus.Deleted:
                            offset++;
                            continue;
                        default:
                            result = new ConsumeResult<byte[]?, byte[]?>
                            {
                                IsPartitionEOF = false,
                                Message = storedMessage.Message,
                                Offset = offset,
                                Partition = Partition,
                                Topic = Topic.Name
                            };
                            return true;
                    }
                }
            }
        }

        public bool HasMessages(Offset offset) => TryPull(Math.Max(0, offset), out _);

        public IReadOnlyCollection<Message<byte[]?, byte[]?>> GetAllMessages() =>
            _messages
                .Where(message => message.Status == StoredMessageStatus.Committed)
                .Select(message => message.Message)
                .AsReadOnlyCollection();

        public void CommitTransaction(Guid transactionUniqueId)
        {
            lock (_messages)
            {
                Transaction? transaction =
                    _transactions.FirstOrDefault(transaction => transaction.Id == transactionUniqueId);

                if (transaction == null)
                    return;

                foreach (StoredMessage message in transaction.Messages)
                {
                    message.Status = StoredMessageStatus.Committed;
                }

                _transactions.Remove(transaction);
                TotalMessagesCount += transaction.Messages.Count;
            }
        }

        public void AbortTransaction(Guid transactionUniqueId)
        {
            lock (_messages)
            {
                Transaction? transaction =
                    _transactions.FirstOrDefault(transaction => transaction.Id == transactionUniqueId);

                if (transaction == null)
                    return;

                foreach (StoredMessage message in transaction.Messages)
                {
                    message.Status = StoredMessageStatus.Deleted;
                }

                _transactions.Remove(transaction);
                TotalMessagesCount -= transaction.Messages.Count;
            }
        }

        private class StoredMessage
        {
            public StoredMessage(Message<byte[]?, byte[]?> message, Offset offset, StoredMessageStatus status)
            {
                Message = message;
                Offset = offset;
                Status = status;
            }

            public Message<byte[]?, byte[]?> Message { get; }

            public Offset Offset { get; }

            public StoredMessageStatus Status { get; set; }
        }

        private record Transaction(Guid Id)
        {
            public List<StoredMessage> Messages { get; } = new();
        }
    }
}
