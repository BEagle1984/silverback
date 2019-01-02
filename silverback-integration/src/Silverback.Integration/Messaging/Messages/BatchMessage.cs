using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Messages
{
    public abstract class BatchMessage : IMessage
    {
        protected BatchMessage(Guid batchId, IEnumerable<IMessage> messages)
        {
            Messages = messages;
            BatchId = batchId;
            BatchSize = messages.Count();
        }

        public Guid BatchId { get; }

        public IEnumerable<IMessage> Messages { get; }

        public int BatchSize { get; }
    }
}