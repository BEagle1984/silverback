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
                    message.EndpointName,
                    message.ActualEndpointName))
            .ToList();

        /// <inheritdoc cref="IOutboxReader.AcknowledgeAsync(OutboxStoredMessage)" />
        public async Task AcknowledgeAsync(OutboxStoredMessage outboxMessage)
        {
            if (await RemoveMessageAsync(outboxMessage).ConfigureAwait(false))
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc cref="IOutboxReader.AcknowledgeAsync(IEnumerable{OutboxStoredMessage})" />
        public async Task AcknowledgeAsync(IEnumerable<OutboxStoredMessage> outboxMessages)
        {
            bool removed = false;

            foreach (var message in outboxMessages)
            {
                removed |= await RemoveMessageAsync(message).ConfigureAwait(false);
            }

            if (removed)
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc cref="IOutboxReader.RetryAsync(OutboxStoredMessage)" />
        public Task RetryAsync(OutboxStoredMessage outboxMessage)
        {
            // Nothing to do, the message is retried if not acknowledged
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="IOutboxReader.RetryAsync(IEnumerable{OutboxStoredMessage})" />
        public Task RetryAsync(IEnumerable<OutboxStoredMessage> outboxMessages)
        {
            // Nothing to do, the message is retried if not acknowledged
            return Task.CompletedTask;
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
                return JsonSerializer.Deserialize<IEnumerable<MessageHeader>>(
                           outboxMessage.SerializedHeaders) ??
                       throw new InvalidOperationException("Failed to deserialize message headers.");
            }

#pragma warning disable CS0618 // Obsolete
            if (outboxMessage.Headers != null)
            {
                return JsonSerializer.Deserialize<IEnumerable<MessageHeader>>(outboxMessage.Headers) ??
                       throw new InvalidOperationException("Failed to deserialize message headers.");
            }
#pragma warning restore CS0618 // Obsolete

            throw new InvalidOperationException("Both SerializedHeaders and Headers are null.");
        }

        private async Task<bool> RemoveMessageAsync(OutboxStoredMessage outboxMessage)
        {
            if (outboxMessage is not DbOutboxStoredMessage dbOutboxMessage)
                throw new InvalidOperationException("A DbOutboxStoredMessage is expected.");

            var entity = await DbSet.FindAsync(dbOutboxMessage.Id).ConfigureAwait(false);

            if (entity == null)
                return false;

            DbSet.Remove(entity);
            return true;
        }
    }
}
