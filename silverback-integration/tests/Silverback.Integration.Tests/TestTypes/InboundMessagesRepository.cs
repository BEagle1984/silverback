﻿using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Repositories;

namespace Silverback.Tests.TestTypes
{
    public class InboundMessagesRepository : IInboundMessagesRepository<InboundMessageEntity>
    {
        public HashSet<InboundMessageEntity> DbSet { get; } = new HashSet<InboundMessageEntity>();

        public InboundMessageEntity Create()
            => new InboundMessageEntity();

        public void Add(InboundMessageEntity entity)
            => DbSet.Add(entity);

        public bool Exists(Guid messageId)
            => DbSet.Any(m => m.MessageId == messageId);

        public void SaveChanges()
        {
            // Nothing to do
        }
    }
}