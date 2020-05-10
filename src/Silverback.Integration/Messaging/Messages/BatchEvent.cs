// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The base class for the events related to the batch processing.
    /// </summary>
    public abstract class BatchEvent : ISilverbackEvent
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BatchEvent" /> class.
        /// </summary>
        /// <param name="batchId"> The unique identifier of the batch. </param>
        /// <param name="envelopes">
        ///     The collection of envelopes that belong to the batch.
        /// </param>
        protected BatchEvent(Guid batchId, IReadOnlyCollection<IRawInboundEnvelope> envelopes)
        {
            Envelopes = envelopes;
            BatchId = batchId;
            BatchSize = envelopes?.Count ?? 0;
        }

        /// <summary> Gets the unique identifier of the batch of messages. </summary>
        public Guid BatchId { get; }

        /// <summary>
        ///     Gets the collection of envelopes containing the messages that belong to the batch.
        /// </summary>
        public IReadOnlyCollection<IRawInboundEnvelope> Envelopes { get; }

        /// <summary> Gets the number of messags in the batch. </summary>
        public int BatchSize { get; }
    }
}
