// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    public abstract class BatchEvent : ISilverbackEvent
    {
        protected BatchEvent(Guid batchId, IReadOnlyCollection<IRawInboundEnvelope> envelopes)
        {
            Envelopes = envelopes;
            BatchId = batchId;
            BatchSize = envelopes?.Count ?? 0;
        }

        public Guid BatchId { get; }

        public IReadOnlyCollection<IRawInboundEnvelope> Envelopes { get; }

        public int BatchSize { get; }
    }
}