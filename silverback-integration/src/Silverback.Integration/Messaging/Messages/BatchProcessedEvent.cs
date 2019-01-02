// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    public class BatchProcessedEvent : BatchMessage, IEvent
    {
        public BatchProcessedEvent(Guid batchId, IEnumerable<IMessage> messages) : base(batchId, messages)
        {
        }
    }
}
