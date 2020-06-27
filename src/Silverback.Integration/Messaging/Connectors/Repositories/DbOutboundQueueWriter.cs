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

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <summary>
    ///     Stores the outbound messages into the database. Used by the <see cref="DeferredOutboundConnector" />
    ///     .
    /// </summary>
    public class DbOutboundQueueWriter : RepositoryBase<OutboundMessage>, IOutboundQueueWriter
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DbOutboundQueueWriter" /> class.
        /// </summary>
        /// <param name="dbContext">
        ///     The <see cref="IDbContext" /> to use as storage.
        /// </param>
        public DbOutboundQueueWriter(IDbContext dbContext)
            : base(dbContext)
        {
        }

        /// <inheritdoc cref="IOutboundQueueWriter.Enqueue" />
        public Task Enqueue(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            DbSet.Add(
                new OutboundMessage
                {
                    Content = envelope.RawMessage ??
                              envelope.Endpoint.Serializer.Serialize(
                                  envelope.Message,
                                  envelope.Headers,
                                  new MessageSerializationContext(envelope.Endpoint)),
                    SerializedHeaders =
                        JsonSerializer.SerializeToUtf8Bytes((IEnumerable<MessageHeader>)envelope.Headers),
                    EndpointName = envelope.Endpoint.Name,
                    Created = DateTime.UtcNow
                });

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="IOutboundQueueWriter.Commit" />
        public Task Commit()
        {
            // Nothing to do, the transaction is implicitly committed calling `SaveChanges` on the DbContext.
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="IOutboundQueueWriter.Rollback" />
        public Task Rollback()
        {
            // Nothing to do, the transaction is aborted by the DbContext
            return Task.CompletedTask;
        }
    }
}
