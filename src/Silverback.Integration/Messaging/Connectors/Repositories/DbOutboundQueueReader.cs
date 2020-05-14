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
    /// <summary>
    ///     <para>
    ///         Exposes the methods to read from the outbound queue. Used by the <see cref="IOutboundQueueWorker"/>.
    ///     </para>
    ///     <para>
    ///         An <see cref="IDbContext" /> is used to read from a queue stored in a database table.
    ///     </para>
    /// </summary>
    // TODO: Test
    public class DbOutboundQueueReader : RepositoryBase<OutboundMessage>, IOutboundQueueReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbOutboundQueueReader"/> class.
        /// </summary>
        /// <param name="dbContext">
        ///     The <see cref="IDbContext" /> to use as storage.
        /// </param>
        public DbOutboundQueueReader(IDbContext dbContext)
            : base(dbContext)
        {
        }

        /// <inheritdoc />
        public async Task<TimeSpan> GetMaxAge()
        {
            var oldestCreated = await DbSet.AsQueryable()
                .OrderBy(m => m.Created)
                .Select(m => m.Created)
                .FirstOrDefaultAsync();

            if (oldestCreated == default)
                return TimeSpan.Zero;

            return DateTime.UtcNow - oldestCreated;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<QueuedMessage>> Dequeue(int count) =>
            (await DbSet.AsQueryable()
                .OrderBy(m => m.Id)
                .Take(count)
                .ToListAsync())
            .Select(message => new DbQueuedMessage(
                message.Id,
                message.Content,
                DefaultSerializer.Deserialize<IEnumerable<MessageHeader>>(message.Headers),
                DefaultSerializer.Deserialize<IProducerEndpoint>(message.Endpoint)))
            .ToList();

        /// <inheritdoc />
        public Task Retry(QueuedMessage queuedMessage)
        {
            // Nothing to do, the message is retried if not marked as produced
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task Acknowledge(QueuedMessage queuedMessage)
        {
            if (!(queuedMessage is DbQueuedMessage dbQueuedMessage))
                throw new InvalidOperationException("A DbQueuedMessage is expected.");

            var entity = await DbSet.FindAsync(dbQueuedMessage.Id);
            DbSet.Remove(entity);

            await DbContext.SaveChangesAsync();
        }

        /// <inheritdoc />
        public Task<int> GetLength() => DbSet.AsQueryable().CountAsync();
    }
}