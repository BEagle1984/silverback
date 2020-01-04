﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class DbQueuedMessage : QueuedMessage
    {
        public DbQueuedMessage(int id, byte[] content, IEnumerable<MessageHeader> headers, IProducerEndpoint endpoint)
            : base(content, headers, endpoint)
        {
            Id = id;
        }

        public int Id { get; }
    }
}