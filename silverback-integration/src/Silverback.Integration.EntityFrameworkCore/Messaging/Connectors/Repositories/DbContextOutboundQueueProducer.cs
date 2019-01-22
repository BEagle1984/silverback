// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Connectors.Model;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class DbContextOutboundQueueProducer : RepositoryBase<OutboundMessage>, IOutboundQueueProducer
    {
        private readonly ILogger _logger;
        public DbContextOutboundQueueProducer(DbContext dbContext, ILogger logger) : base(dbContext)
        {
            _logger = logger;
        }

        public async Task Enqueue(object message, IEndpoint endpoint)
        {
            await DbSet.AddAsync(new OutboundMessage
            {
                Message = Serialize(message),
                EndpointName = endpoint.Name,
                Endpoint = Serialize(endpoint),
                Created = DateTime.UtcNow
            });
        }

        public Task Commit()
        {
            // Nothing to do, the transaction is committed with the DbContext.SaveChanges()
            return Task.CompletedTask;
        }

        public Task Rollback()
        {
            // Nothing to do, the transaction is aborted by the DbContext
            return Task.CompletedTask;
        }
    }
}
