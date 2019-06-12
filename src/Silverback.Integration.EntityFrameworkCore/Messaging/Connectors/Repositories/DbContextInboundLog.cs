// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Silverback.Infrastructure;
using Silverback.Messaging.Connectors.Model;
using MessageKeyProvider = Silverback.Messaging.Messages.MessageKeyProvider;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class DbContextInboundLog : RepositoryBase<InboundMessage>, IInboundLog
    {
        private readonly MessageKeyProvider _messageKeyProvider;
        private readonly object _lock = new object();

        public DbContextInboundLog(DbContext dbContext, MessageKeyProvider messageKeyProvider) : base(dbContext)
        {
            _messageKeyProvider = messageKeyProvider;
        }

        public void Add(object message, IEndpoint endpoint)
        {
            lock (_lock)
            {
                DbSet.Add(new InboundMessage
                {
                    MessageId = _messageKeyProvider.GetKey(message),
                    Message = DefaultSerializer.Serialize(message),
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
            // Nothing to do, just not saving the changes made to the DbContext
        }

        public bool Exists(object message, IEndpoint endpoint)
        {
            var key = _messageKeyProvider.GetKey(message);
            return DbSet.Any(m => m.MessageId == key && m.EndpointName == endpoint.Name);
        }

        public int Length => DbSet.Count();
    }
}