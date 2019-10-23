// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Database;
using Silverback.Infrastructure;
using Silverback.Messaging.Messages;
using OutboundMessage = Silverback.Messaging.Connectors.Model.OutboundMessage;

namespace Silverback.Messaging.Connectors.Repositories
{
    // TODO: Test
    public class DbOutboundQueueConsumer : RepositoryBase<OutboundMessage>, IOutboundQueueConsumer
    {
        private readonly bool _removeProduced;

        public DbOutboundQueueConsumer(IDbContext dbContext, bool removeProduced) : base(dbContext)
        {
            _removeProduced = removeProduced;
        }

        public async Task<TimeSpan> GetMaxAge()
        {
            var oldestCreated = await DbSet.AsQueryable()
                .Where(m => m.Produced == null)
                .Select(m => m.Created)
                .DefaultIfEmpty(default)
                .MinAsync();

            if (oldestCreated == default)
                return TimeSpan.Zero;

            return DateTime.UtcNow - oldestCreated;
        }

        public async Task<IEnumerable<QueuedMessage>> Dequeue(int count) =>
            (await DbSet.AsQueryable()
                    .Where(m => m.Produced == null)
                    .OrderBy(m => m.Id)
                    .Take(count)
                    .ToListAsync())
            .Select(message => new DbQueuedMessage(
                message.Id,
                message.Content,
                DefaultSerializer.Deserialize<IEnumerable<MessageHeader>>(message.Headers),
                DefaultSerializer.Deserialize<IEndpoint>(message.Endpoint)));

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

        public Task<int> GetLength() => DbSet.AsQueryable().CountAsync(m => m.Produced == null);
    }
}