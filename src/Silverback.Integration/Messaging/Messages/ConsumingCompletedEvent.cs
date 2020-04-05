// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Fired when the processing of a consumed message is successfully completed.
    /// </summary>
    public class ConsumingCompletedEvent : ConsumingEvent
    {
        public ConsumingCompletedEvent(IReadOnlyCollection<IRawInboundEnvelope> envelopes)
            : base(envelopes)
        {
        }
    }
}