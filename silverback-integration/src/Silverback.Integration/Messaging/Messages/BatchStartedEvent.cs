// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// The event fired just before the first message in a new batch is published.
    /// </summary>
    public class BatchStartedEvent : BatchEvent
    {
        public BatchStartedEvent(Guid batchId) : base(batchId, null)
        {
        }
    }
}
