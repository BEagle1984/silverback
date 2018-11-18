using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Silverback.Messaging.Connectors.Model;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    // TODO: Test
    public class DbContextOutboundQueueConsumer : RepositoryBase<OutboundMessage>, IOutboundQueueConsumer
    {
        private const bool RemoveProduced = false; // TODO: Parameter?

        public DbContextOutboundQueueConsumer(DbContext dbContext) : base(dbContext)
        {
        }

        public IEnumerable<QueuedMessage> Dequeue(int count) => DbSet
            .Where(m => m.Produced == null)
            .OrderBy(m => m.Created)
            .Take(count)
            .ToList()
            .Select(message => new QueuedMessage(
                Deserialize<IIntegrationMessage>(message.Message),
                Deserialize<IEndpoint>(message.Endpoint)));

        private T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, SerializerSettings);

        public void Retry(QueuedMessage queuedMessage)
        {
            // Nothing to do, the message is retried if not marked as produced
        }

        public void Acknowledge(QueuedMessage queuedMessage)
        {
            var entity = DbSet.Find(queuedMessage.Message.Id);

            if (RemoveProduced)
                DbSet.Remove(entity);
            else
                entity.Produced = DateTime.Now;

            DbContext.SaveChanges();
        }

        public int Length => DbSet.Count(m => m.Produced == null);
    }
}