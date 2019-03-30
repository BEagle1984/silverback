// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class DbQueuedMessage : QueuedMessage
    {
        public DbQueuedMessage(int id, IOutboundMessage message) : base(message)
        {
            Id = id;
        }

        public int Id { get; }
    }
}
