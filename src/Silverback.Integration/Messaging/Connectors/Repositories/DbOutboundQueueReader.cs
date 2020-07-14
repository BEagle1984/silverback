// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Silverback.Database;
using Silverback.Database.Model;
using Silverback.Infrastructure;
using Silverback.Messaging.Connectors.Repositories.Model;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <summary>
    ///     <para>
    ///         Exposes the methods to read from the outbound queue. Used by the
    ///         <see cref="IOutboundQueueWorker" />.
    ///     </para>
    ///     <para>
    ///         An <see cref="IDbContext" /> is used to read from a queue stored in a database table.
    ///     </para>
    /// </summary>
    // TODO: Test
    public class DbOutboundQueueReader : RepositoryBase<OutboundMessage>, IOutboundQueueReader
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DbOutboundQueueReader" /> class.
        /// </summary>
        /// <param name="dbContext">
        ///     The <see cref="IDbContext" /> to use as storage.
        /// </param>
        public DbOutboundQueueReader(IDbContext dbContext)
            : base(dbContext)
        {
        }

        /// <inheritdoc cref="IOutboundQueueReader.GetMaxAge" />
        public async Task<TimeSpan> GetMaxAge()
        {
            var oldestCreated = await DbSet.AsQueryable()
                .OrderBy(m => m.Created)
                .Select(m => m.Created)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (oldestCreated == default)
                return TimeSpan.Zero;

            return DateTime.UtcNow - oldestCreated;
        }

        /// <inheritdoc cref="IOutboundQueueReader.Dequeue" />
        public async Task<IReadOnlyCollection<QueuedMessage>> Dequeue(int count) =>
            (await DbSet.AsQueryable()
                .OrderBy(m => m.Id)
                .Take(count)
                .ToListAsync()
                .ConfigureAwait(false))
            .Select(
                message => new DbQueuedMessage(
                    message.Id,
                    GetMessageType(message),
                    message.Content,
                    DeserializeHeaders(message),
                    message.EndpointName))
            .ToList();

        /// <inheritdoc cref="IOutboundQueueReader.Retry" />
        public Task Retry(QueuedMessage queuedMessage)
        {
            // Nothing to do, the message is retried if not acknowledged
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="IOutboundQueueReader.Acknowledge" />
        public async Task Acknowledge(QueuedMessage queuedMessage)
        {
            if (!(queuedMessage is DbQueuedMessage dbQueuedMessage))
                throw new InvalidOperationException("A DbQueuedMessage is expected.");

            var entity = await DbSet.FindAsync(dbQueuedMessage.Id).ConfigureAwait(false);

            if (entity == null)
                return;

            DbSet.Remove(entity);

            await DbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc cref="IOutboundQueueReader.GetLength" />
        public Task<int> GetLength() => DbSet.AsQueryable().CountAsync();

        private static Type? GetMessageType(OutboundMessage message)
        {
            if (message.MessageType == null)
                return null;

            return TypesCache.GetType(message.MessageType);
        }

        private static IEnumerable<MessageHeader> DeserializeHeaders(OutboundMessage message)
        {
            if (message.SerializedHeaders != null)
            {
                return JsonSerializer.Deserialize<IEnumerable<MessageHeader>>(message.SerializedHeaders);
            }

#pragma warning disable 618
            if (message.Headers != null)
            {
                return JsonSerializer.Deserialize<IEnumerable<MessageHeader>>(message.Headers);
            }
#pragma warning restore 618

            throw new InvalidOperationException("Both SerializedHeaders and Headers are null.");
        }
    }
}
