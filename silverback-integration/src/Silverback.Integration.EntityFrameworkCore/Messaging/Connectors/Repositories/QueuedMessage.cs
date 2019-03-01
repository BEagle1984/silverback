// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)
namespace Silverback.Messaging.Connectors.Repositories
{
    public class DbQueuedMessage : QueuedMessage
    {
        public DbQueuedMessage(int id, object message, IEndpoint endpoint) : base(message, endpoint)
        {
            Id = id;
        }

        public int Id { get; }
    }
}
