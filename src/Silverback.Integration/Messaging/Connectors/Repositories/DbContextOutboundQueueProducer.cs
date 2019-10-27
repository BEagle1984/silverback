// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Database;
using Silverback.Infrastructure;
using Silverback.Messaging.Messages;
using OutboundMessage = Silverback.Messaging.Connectors.Model.OutboundMessage;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class DbOutboundQueueProducer : RepositoryBase<OutboundMessage>, IOutboundQueueProducer
    {
        public DbOutboundQueueProducer(IDbContext dbContext) : base(dbContext)
        {
        }

        public Task Enqueue(IOutboundMessage message)
        {
            DbSet.Add(new OutboundMessage
            {
                Content = message.RawContent ?? message.Endpoint.Serializer.Serialize(message.Content, message.Headers),
                Headers = DefaultSerializer.Serialize((IEnumerable<MessageHeader>) message.Headers),
                Endpoint = DefaultSerializer.Serialize(message.Endpoint),
                EndpointName = message.Endpoint.Name,
                Created = DateTime.UtcNow
            });

            return Task.CompletedTask;
        }

        public Task Commit() => Task.CompletedTask;

        // Nothing to do, the transaction is aborted by the DbContext
        public Task Rollback() => Task.CompletedTask;
    }
}
