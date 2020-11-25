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
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories.Model;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.TransactionalOutbox.Repositories
{
    /// <summary>
    ///     <para>
    ///         Exposes the methods to read from the outbound queue. Used by the
    ///         <see cref="IOutboxWorker" />.
    ///     </para>
    ///     <para>
    ///         An <see cref="IDbContext" /> is used to read from a queue stored in a database table.
    ///     </para>
    /// </summary>
    public class DbOutboxReader : RepositoryBase<OutboxMessage>, IOutboxReader
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DbOutboxReader" /> class.
        /// </summary>
        /// <param name="dbContext">
        ///     The <see cref="IDbContext" /> to use as storage.
        /// </param>
        public DbOutboxReader(IDbContext dbContext)
            : base(dbContext)
        {
        }

        /// <inheritdoc cref="IOutboxReader.GetMaxAgeAsync" />
        public async Task<TimeSpan> GetMaxAgeAsync()
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

        /// <inheritdoc cref="IOutboxReader.ReadAsync" />
        public async Task<IReadOnlyCollection<OutboxStoredMessage>> ReadAsync(int count) =>
            (await DbSet.AsQueryable()
                .OrderBy(m => m.Id)
                .Take(count)
                .ToListAsync()
                .ConfigureAwait(false))
            .Select(
                message => new DbOutboxStoredMessage(
                    message.Id,
                    GetMessageType(message),
                    message.Content,
                    DeserializeHeaders(message),
                    message.EndpointName))
            .ToList();

        /// <inheritdoc cref="IOutboxReader.RetryAsync" />
        public Task RetryAsync(OutboxStoredMessage outboxMessage)
        {
            // Nothing to do, the message is retried if not acknowledged
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="IOutboxReader.AcknowledgeAsync" />
        public async Task AcknowledgeAsync(OutboxStoredMessage outboxMessage)
        {
            if (!(outboxMessage is DbOutboxStoredMessage dbOutboxMessage))
                throw new InvalidOperationException("A DbOutboxStoredMessage is expected.");

            var entity = await DbSet.FindAsync(dbOutboxMessage.Id).ConfigureAwait(false);

            if (entity == null)
                return;

            DbSet.Remove(entity);

            await DbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc cref="IOutboxReader.GetLengthAsync" />
        public Task<int> GetLengthAsync() => DbSet.AsQueryable().CountAsync();

        private static Type? GetMessageType(OutboxMessage outboxMessage)
        {
            if (outboxMessage.MessageType == null)
                return null;

            return TypesCache.GetType(outboxMessage.MessageType);
        }

        private static IEnumerable<MessageHeader> DeserializeHeaders(OutboxMessage outboxMessage)
        {
            if (outboxMessage.SerializedHeaders != null)
            {
                return JsonSerializer.Deserialize<IEnumerable<MessageHeader>>(outboxMessage.SerializedHeaders);
            }

#pragma warning disable 618
            if (outboxMessage.Headers != null)
            {
                return JsonSerializer.Deserialize<IEnumerable<MessageHeader>>(outboxMessage.Headers);
            }
#pragma warning restore 618

            throw new InvalidOperationException("Both SerializedHeaders and Headers are null.");
        }
    }
}
