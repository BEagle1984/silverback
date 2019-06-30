// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Messages
{
    public abstract class BatchEvent : ISilverbackEvent
    {
        protected BatchEvent(Guid batchId, IEnumerable<IInboundMessage> messages)
        {
            Messages = messages;
            BatchId = batchId;
            BatchSize = messages?.Count() ?? 0;
        }

        public Guid BatchId { get; }

        public IEnumerable<IInboundMessage> Messages { get; }

        public int BatchSize { get; }
    }
}