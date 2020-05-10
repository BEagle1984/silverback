// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The event published when all the messages in a batch have been successfully processed.
    /// </summary>
    public class BatchProcessedEvent : BatchEvent
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BatchProcessedEvent" /> class.
        /// </summary>
        /// <param name="batchId"> The unique identifier of the batch. </param>
        /// <param name="envelopes">
        ///     The collection of envelopes that belong to the batch.
        /// </param>
        public BatchProcessedEvent(Guid batchId, IReadOnlyCollection<IRawInboundEnvelope> envelopes)
            : base(batchId, envelopes)
        {
        }
    }
}
