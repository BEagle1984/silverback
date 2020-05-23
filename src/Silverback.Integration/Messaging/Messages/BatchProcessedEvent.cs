﻿// Copyright (c) 2020 Sergio Aquilini
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
        public BatchProcessedEvent(Guid batchId, IReadOnlyCollection<IRawInboundEnvelope> envelopes)
            : base(batchId, envelopes)
        {
        }
    }
}