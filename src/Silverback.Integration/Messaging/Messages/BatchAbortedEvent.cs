// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// The event fired when an exception occured during the processing of a batch.
    /// </summary>
    public class BatchAbortedEvent : BatchEvent
    {
        public BatchAbortedEvent(Guid batchId, IEnumerable<IRawInboundMessage> messages, Exception exception)
            : base(batchId, messages)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}