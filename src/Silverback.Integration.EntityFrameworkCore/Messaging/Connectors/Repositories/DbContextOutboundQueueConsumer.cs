// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public async Task<IEnumerable<QueuedMessage>> Dequeue(int count) =>
            (await DbSet
                .Where(m => m.Produced == null)
                .OrderBy(m => m.Id)
                .Take(count)
                .ToListAsync())
            .Select(message => new DbQueuedMessage(
                message.Id,
                DefaultSerializer.Deserialize<IOutboundMessage>(message.Message)));

        public Task Retry(QueuedMessage queuedMessage)
        {
            // Nothing to do, the message is retried if not marked as produced
            return Task.CompletedTask;
        }

        public async Task Acknowledge(QueuedMessage queuedMessage)
        {
            if (!(queuedMessage is DbQueuedMessage dbQueuedMessage))
                throw new InvalidOperationException("A DbQueuedMessage is expected.");

            var entity = await DbSet.FindAsync(dbQueuedMessage.Id);

            if (_removeProduced)
                DbSet.Remove(entity);
            else
                entity.Produced = DateTime.UtcNow;

            await DbContext.SaveChangesAsync();
        }

        public int Length => DbSet.Count(m => m.Produced == null);
    }
}