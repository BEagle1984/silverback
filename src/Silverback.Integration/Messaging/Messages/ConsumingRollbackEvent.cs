// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Fired when an exception occurs while processing a consumed message.
    /// </summary>
    public class ConsumingAbortedEvent : ConsumingEvent
    {
        public ConsumingAbortedEvent(IReadOnlyCollection<IRawInboundEnvelope> envelopes) : base(envelopes)
        {
        }
    }
}