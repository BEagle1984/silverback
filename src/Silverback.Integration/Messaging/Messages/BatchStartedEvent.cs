// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The event published just before the first message in a new batch is published.
    /// </summary>
    public class BatchStartedEvent : BatchEvent
    {
        public BatchStartedEvent(Guid batchId)
            : base(batchId, null)
        {
        }
    }
}