// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Connectors.Model;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    // TODO: Test
    public class DbContextOutboundQueueConsumer : RepositoryBase<OutboundMessage>, IOutboundQueueConsumer
    {
        private readonly MessageKeyProvider _messageKeyProvider;
        private readonly bool _removeProduced;

        public DbContextOutboundQueueConsumer(DbContext dbContext, MessageKeyProvider messageKeyProvider,
            bool removeProduced) : base(dbContext)
        {
            _removeProduced = removeProduced;
            _messageKeyProvider = messageKeyProvider;
        }

        public IEnumerable<QueuedMessage> Dequeue(int count) => DbSet
            .Where(m => m.Produced == null)
            .OrderBy(m => m.Created)
            .Take(count)
            .ToList()
            .Select(message => new QueuedMessage(
                Deserialize<object>(message.Message),
                Deserialize<IEndpoint>(message.Endpoint)));

        public void Retry(QueuedMessage queuedMessage)
        {
            // Nothing to do, the message is retried if not marked as produced
        }

        public void Acknowledge(QueuedMessage queuedMessage)
        {
            var key = _messageKeyProvider.GetKey(queuedMessage.Message);
            var entity = DbSet.Find(key);

            if (_removeProduced)
                DbSet.Remove(entity);
            else
                entity.Produced = DateTime.UtcNow;

            DbContext.SaveChanges();
        }

        public int Length => DbSet.Count(m => m.Produced == null);
    }
}