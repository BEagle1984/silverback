// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The event published when enough messages have been added to the batch or the timeout has been
    ///     reached and the batch is about to be processed.
    /// </summary>
    public class BatchCompleteEvent : BatchEvent
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BatchCompleteEvent" /> class.
        /// </summary>
        /// <param name="batchId">
        ///     The unique identifier of the batch.
        /// </param>
        /// <param name="envelopes">
        ///     The collection of envelopes that belong to the batch.
        /// </param>
        public BatchCompleteEvent(Guid batchId, IReadOnlyCollection<IRawInboundEnvelope> envelopes)
            : base(batchId, envelopes)
        {
        }
    }
}
