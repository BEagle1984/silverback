// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka.Mocks
{
    [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
    internal class InMemoryTransactionManager
    {
        private readonly InMemoryTopicCollection _topics;

        private readonly List<TransactionalProducerInfo> _producersInfo = new();

        public InMemoryTransactionManager(InMemoryTopicCollection topics)
        {
            _topics = topics;
        }

        public Guid InitTransaction(string transactionalId)
        {
            Check.NotNull(transactionalId, nameof(transactionalId));

            lock (_producersInfo)
            {
                TransactionalProducerInfo? producerInfo =
                    _producersInfo.FirstOrDefault(info => info.TransactionalId == transactionalId);

                if (producerInfo == null)
                {
                    producerInfo = new TransactionalProducerInfo(transactionalId);
                    _producersInfo.Add(producerInfo);
                }
                else
                {
                    _topics.ForEach(topic => topic.AbortTransaction(producerInfo.TransactionalUniqueId));
                    producerInfo.TransactionalUniqueId = Guid.NewGuid();
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

            TransactionalProducerInfo? producerInfo =
                _producersInfo.FirstOrDefault(info => info.TransactionalUniqueId == transactionalUniqueId);

            if (producerInfo == null)
                throw new InvalidOperationException("The producer has been fenced.");

            return producerInfo;
        }

        private class TransactionalProducerInfo
        {
            public TransactionalProducerInfo(string transactionalId)
            {
                this.TransactionalId = transactionalId;
            }

            public string TransactionalId { get; }

            public Guid TransactionalUniqueId { get; set; } = Guid.NewGuid();

            public bool IsTransactionPending { get; set; }
        }
    }
}
