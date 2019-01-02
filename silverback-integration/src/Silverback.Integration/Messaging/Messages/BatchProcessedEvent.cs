using System;
using System.Collections.Generic;
using System.Text;

namespace Silverback.Messaging.Messages
{
    public class BatchProcessedEvent : BatchMessage, IEvent
    {
        public BatchProcessedEvent(Guid batchId, IEnumerable<IMessage> messages) : base(batchId, messages)
        {
        }
    }
}
