// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Connectors.Model;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class DbContextInboundLog : RepositoryBase<InboundMessage>, IInboundLog
    {
        private object _lock = new object();

        public DbContextInboundLog(DbContext dbContext) : base(dbContext)
        {
        }

        public void Add(IIntegrationMessage message, IEndpoint endpoint)
        {
            lock (_lock)
            {
                DbSet.Add(new InboundMessage
                {
                    MessageId = message.Id,
                    Message = Serialize(message),
                    EndpointName = endpoint.Name,
                    Consumed = DateTime.UtcNow
                });
            }
        }

        public void Commit()
        {
            lock (_lock)
            {
                // Call SaveChanges, in case it isn't called by a subscriber
                DbContext.SaveChanges();
            }
        }

        public void Rollback()
        {
            // Nothing to do, just not saving the DbContext
        }

        public bool Exists(IIntegrationMessage message, IEndpoint endpoint)
        {
            lock (_lock)
            {
               return DbSet.Any(m => m.MessageId == message.Id && m.EndpointName == endpoint.Name);
            }
        }

        public int Length => DbSet.Count();
    }
}