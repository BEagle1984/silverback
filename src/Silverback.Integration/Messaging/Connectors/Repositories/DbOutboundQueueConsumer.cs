// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Database;
using Silverback.Infrastructure;
using Silverback.Messaging.Connectors.Model;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    // TODO: Test
    public class DbOutboundQueueConsumer : RepositoryBase<OutboundMessage>, IOutboundQueueConsumer
    {
        public DbOutboundQueueConsumer(IDbContext dbContext)
            : base(dbContext)
        {
        }

        public async Task<TimeSpan> GetMaxAge()
        {
            var oldestCreated = await DbSet.AsQueryable()
                .Where(m => m.Produced == null)
                .OrderBy(m => m.Created)
                .Select(m => m.Created)
                .FirstOrDefaultAsync();

            if (oldestCreated == default)
                return TimeSpan.Zero;

            return DateTime.UtcNow - oldestCreated;
        }

        public async Task<IReadOnlyCollection<QueuedMessage>> Dequeue(int count) =>
            (await DbSet.AsQueryable()
                .Where(m => m.Produced == null)
                .OrderBy(m => m.Id)
                .Take(count)
                .ToListAsync())
            .Select(message => new DbQueuedMessage(
                message.Id,
                message.Content,
                DefaultSerializer.Deserialize<IEnumerable<MessageHeader>>(message.Headers),
                DefaultSerializer.Deserialize<IProducerEndpoint>(message.Endpoint)))
            .ToList();

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
            DbSet.Remove(entity);

            await DbContext.SaveChangesAsync();
        }

        public Task<int> GetLength() => DbSet.AsQueryable().CountAsync(m => m.Produced == null);
    }
}