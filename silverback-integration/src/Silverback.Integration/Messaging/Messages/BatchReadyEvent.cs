using System;
using System.Collections.Generic;
using Silverback.Messaging.Batch;

namespace Silverback.Messaging.Messages
{
    public class BatchReadyEvent : BatchMessage, IEvent
    {
        public BatchReadyEvent(Guid batchId, IEnumerable<IMessage> messages) : base(batchId, messages)
        {
        }
    }
}