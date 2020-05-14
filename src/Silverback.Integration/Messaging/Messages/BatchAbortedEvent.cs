// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The event published when an exception occured during the processing of a batch.
    /// </summary>
    public class BatchAbortedEvent : BatchEvent
    {
        public BatchAbortedEvent(Guid batchId, IReadOnlyCollection<IRawInboundEnvelope> envelopes, Exception exception)
            : base(batchId, envelopes)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}