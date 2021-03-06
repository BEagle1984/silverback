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
        public Task WriteAsync(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string endpointName,
            string actualEndpointName)
        {
            DbSet.Add(
                new OutboxMessage
                {
                    MessageType = message?.GetType().AssemblyQualifiedName,
                    Content = messageBytes,
                    SerializedHeaders = SerializeHeaders(headers),
                    EndpointName = endpointName,
                    ActualEndpointName = actualEndpointName,
                    Created = DateTime.UtcNow
                });

            return Task.CompletedTask;
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

        private static byte[]? SerializeHeaders(IReadOnlyCollection<MessageHeader>? headers)
        {
            if (headers == null)
                return null;

            return JsonSerializer.SerializeToUtf8Bytes(headers);
        }
    }
}
