// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Database;
using Silverback.Infrastructure;
using Silverback.Messaging.Connectors.Model;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <summary>
    ///     Stores the outbound messages into the database. Used by the <see cref="DeferredOutboundConnector"/>.
    /// </summary>
    public class DbOutboundQueueWriter : RepositoryBase<OutboundMessage>, IOutboundQueueWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbOutboundQueueWriter"/> class.
        /// </summary>
        /// <param name="dbContext">
        ///     The <see cref="IDbContext" /> to use as storage.
        /// </param>
        public DbOutboundQueueWriter(IDbContext dbContext)
            : base(dbContext)
        {
        }

        /// <inheritdoc />
        public Task Enqueue(IOutboundEnvelope envelope)
        {
            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            DbSet.Add(
                new OutboundMessage
                {
                    Content = envelope.RawMessage ??
                              envelope.Endpoint.Serializer.Serialize(
                                  envelope.Message,
                                  envelope.Headers,
                                  new MessageSerializationContext(envelope.Endpoint)),
                    Headers = DefaultSerializer.Serialize((IEnumerable<MessageHeader>)envelope.Headers),
                    Endpoint = DefaultSerializer.Serialize(envelope.Endpoint),
                    EndpointName = envelope.Endpoint.Name,
                    Created = DateTime.UtcNow
                });

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Commit()
        {
            // Nothing to do, the transaction is implicitly committed calling `SaveChanges` on the DbContext.
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Rollback()
        {
            // Nothing to do, the transaction is aborted by the DbContext
            return Task.CompletedTask;
        }
    }
}
