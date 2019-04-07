// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Silverback.Infrastructure;
using Silverback.Messaging.Connectors.Model;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    // TODO: Test
    public class DbContextOutboundQueueConsumer : RepositoryBase<OutboundMessage>, IOutboundQueueConsumer
    {
        private readonly bool _removeProduced;

        public DbContextOutboundQueueConsumer(DbContext dbContext, bool removeProduced) : base(dbContext)
        {
            _removeProduced = removeProduced;
        }

        public IEnumerable<QueuedMessage> Dequeue(int count) => DbSet
            .Where(m => m.Produced == null)
            .OrderBy(m => m.Id)
            .Take(count)
            .ToList()
            .Select(message => new DbQueuedMessage(
                message.Id, 
                DefaultSerializer.Deserialize<IOutboundMessage>(message.Message)));

        public void Retry(QueuedMessage queuedMessage)
        {
            // Nothing to do, the message is retried if not marked as produced
        }

        public void Acknowledge(QueuedMessage queuedMessage)
        {
            if (!(queuedMessage is DbQueuedMessage dbQueuedMessage))
                throw new InvalidOperationException("A DbQueuedMessage is expected.");

            var entity = DbSet.Find(dbQueuedMessage.Id);

            if (_removeProduced)
                DbSet.Remove(entity);
            else
                entity.Produced = DateTime.UtcNow;

            DbContext.SaveChanges();
        }

        public int Length => DbSet.Count(m => m.Produced == null);
    }
}