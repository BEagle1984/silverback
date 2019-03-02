// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Silverback.Infrastructure;
using Silverback.Messaging.Connectors.Model;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class DbContextOutboundQueueProducer : RepositoryBase<OutboundMessage>, IOutboundQueueProducer
    {
        public DbContextOutboundQueueProducer(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task Enqueue(object message, IEndpoint endpoint)
        {
            await DbSet.AddAsync(new OutboundMessage
            {
                Message = DefaultSerializer.Serialize(message),
                Endpoint = DefaultSerializer.Serialize(endpoint),
                Created = DateTime.UtcNow
            });
        }

        // Nothing to do, the transaction is committed with the DbContext.SaveChanges()
        public Task Commit() => Task.CompletedTask;

        // Nothing to do, the transaction is aborted by the DbContext
        public Task Rollback() => Task.CompletedTask;
    }
}
