using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    internal class BatchProcessingMessage : BatchMessage, IEvent
    {
        public BatchProcessingMessage(Guid batchId, IEnumerable<IMessage> messages) : base(batchId, messages)
        {
        }
    }
}