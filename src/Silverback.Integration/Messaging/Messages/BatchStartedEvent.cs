// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The event published when the first message of a new batch has been received.
    /// </summary>
    public class BatchStartedEvent : BatchEvent
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BatchStartedEvent" /> class.
        /// </summary>
        /// <param name="batchId"> The unique identifier of the batch. </param>
        /// <param name="envelopes">
        ///     The collection of envelopes that belong to the batch. In this case the collection should contain
        ///     only the first message.
        /// </param>
        public BatchStartedEvent(Guid batchId, IReadOnlyCollection<IRawInboundEnvelope> envelopes)
            : base(batchId, envelopes)
        {
        }
    }
}
