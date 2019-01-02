// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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