// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// The event fired when all the messages in a batch have been published.
    /// </summary>
    public class BatchCompleteEvent : BatchEvent
    {
        public BatchCompleteEvent(Guid batchId, IEnumerable<IInboundMessage> messages) : base(batchId, messages)
        {
        }
    }
}