using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Silverback.Messaging.Connectors.Model;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class DbContextOutboundQueueProducer : RepositoryBase<OutboundMessage>, IOutboundQueueProducer
    {
        public DbContextOutboundQueueProducer(DbContext dbContext) : base(dbContext)
        {
        }

        public Task Enqueue(IIntegrationMessage message, IEndpoint endpoint) =>
            DbSet.AddAsync(CreateEntity(message, endpoint));

        private OutboundMessage CreateEntity(IIntegrationMessage message, IEndpoint endpoint) =>
            new OutboundMessage
            {
                MessageId = message.Id,
                Message = Serialize(message),
                EndpointName = endpoint.Name,
                Endpoint = Serialize(endpoint),
                Created = DateTime.Now
            };

        private string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj, typeof(T), SerializerSettings);
        
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
