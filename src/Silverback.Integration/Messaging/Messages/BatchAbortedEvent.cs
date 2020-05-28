// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The event published when an exception is thrown during the processing of a batch of messages.
    /// </summary>
    public class BatchAbortedEvent : BatchEvent
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BatchAbortedEvent" /> class.
        /// </summary>
        /// <param name="batchId">
        ///     The unique identifier of the batch.
        /// </param>
        /// <param name="envelopes">
        ///     The collection of envelopes that belong to the batch.
        /// </param>
        /// <param name="exception">
        ///     The <see cref="Exception" /> that was thrown while trying to process the batch of messages.
        /// </param>
        public BatchAbortedEvent(Guid batchId, IReadOnlyCollection<IRawInboundEnvelope> envelopes, Exception exception)
            : base(batchId, envelopes)
        {
            Exception = exception;
        }

        /// <summary>
        ///     Gets the <see cref="Exception" /> that was thrown while trying to process the batch of messages.
        /// </summary>
        public Exception Exception { get; }
    }
}
