// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Connectors.Model;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class DbContextOutboundQueueProducer : RepositoryBase<OutboundMessage>, IOutboundQueueProducer
    {
        private readonly MessageKeyProvider _messageKeyProvider;

        public DbContextOutboundQueueProducer(DbContext dbContext, MessageKeyProvider messageKeyProvider) : base(dbContext)
        {
            _messageKeyProvider = messageKeyProvider;
        }

        public Task Enqueue(object message, IEndpoint endpoint) =>
            DbSet.AddAsync(CreateEntity(message, endpoint));

        private OutboundMessage CreateEntity(object message, IEndpoint endpoint)
        {
            _messageKeyProvider.EnsureKeyIsInitialized(message);
            
            return new OutboundMessage
            {
                MessageId = _messageKeyProvider.GetKey(message),
                Message = Serialize(message),
                EndpointName = endpoint.Name,
                Endpoint = Serialize(endpoint),
                Created = DateTime.UtcNow
            };
        }
        
        public Task Commit()
        {
            // Nothing to do, the transaction is committed with the DbContext.SaveChanges()
            return Task.CompletedTask;
        }

        public Task Rollback()
        {
            // Nothing to do, the transaction is rolledback by the DbContext
            return Task.CompletedTask;
        }
    }
}
