// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Silverback.Database;
using Silverback.Database.Model;
using Silverback.Infrastructure;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.TransactionalOutbox.Repositories
{
    /// <summary>
    ///     Stores the outbound messages into the database. Used by the <see cref="OutboxProduceStrategy" />.
    /// </summary>
    public class DbOutboxWriter : RepositoryBase<OutboxMessage>, IOutboxWriter
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DbOutboxWriter" /> class.
        /// </summary>
        /// <param name="dbContext">
        ///     The <see cref="IDbContext" /> to use as storage.
        /// </param>
        public DbOutboxWriter(IDbContext dbContext)
            : base(dbContext)
        {
        }

        /// <inheritdoc cref="IOutboxWriter.WriteAsync" />
        public async Task WriteAsync(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            DbSet.Add(
                new OutboxMessage
                {
                    MessageType = envelope.Message?.GetType().AssemblyQualifiedName,
                    Content = await GetContentAsync(envelope).ConfigureAwait(false),
                    SerializedHeaders =
                        JsonSerializer.SerializeToUtf8Bytes((IEnumerable<MessageHeader>)envelope.Headers),
                    EndpointName = envelope.Endpoint.Name,
                    Created = DateTime.UtcNow
                });
        }

        /// <inheritdoc cref="IOutboxWriter.CommitAsync" />
        public Task CommitAsync()
        {
            // Nothing to do, the transaction is implicitly committed calling `SaveChanges` on the DbContext.
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="IOutboxWriter.RollbackAsync" />
        public Task RollbackAsync()
        {
            // Nothing to do, the transaction is aborted by the DbContext
            return Task.CompletedTask;
        }

        private static async ValueTask<byte[]?> GetContentAsync(IOutboundEnvelope envelope)
        {
            var stream =
                envelope.RawMessage ??
                await envelope.Endpoint.Serializer.SerializeAsync(
                        envelope.Message,
                        envelope.Headers,
                        new MessageSerializationContext(envelope.Endpoint))
                    .ConfigureAwait(false);

            return await stream.ReadAllAsync().ConfigureAwait(false);
        }
    }
}
