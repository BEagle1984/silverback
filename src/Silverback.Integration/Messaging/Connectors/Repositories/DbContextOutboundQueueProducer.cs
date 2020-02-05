// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Database;
using Silverback.Infrastructure;
using Silverback.Messaging.Connectors.Model;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class DbOutboundQueueProducer : RepositoryBase<OutboundMessage>, IOutboundQueueProducer
    {
        public DbOutboundQueueProducer(IDbContext dbContext)
            : base(dbContext)
        {
        }

        public Task Enqueue(IOutboundEnvelope envelope)
        {
            DbSet.Add(new OutboundMessage
            {
                Content = envelope.RawMessage ??
                          envelope.Endpoint.Serializer.Serialize(envelope.Message, envelope.Headers),
                Headers = DefaultSerializer.Serialize((IEnumerable<MessageHeader>) envelope.Headers),
                Endpoint = DefaultSerializer.Serialize(envelope.Endpoint),
                EndpointName = envelope.Endpoint.Name,
                Created = DateTime.UtcNow
            });

            return Task.CompletedTask;
        }

        public Task Commit() => Task.CompletedTask;

        // Nothing to do, the transaction is aborted by the DbContext
        public Task Rollback() => Task.CompletedTask;
    }
}