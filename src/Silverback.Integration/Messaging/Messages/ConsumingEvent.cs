// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    public abstract class ConsumingEvent : ISilverbackEvent
    {
        protected ConsumingEvent(IReadOnlyCollection<IRawInboundEnvelope> envelopes)
        {
            Envelopes = envelopes;
        }

        /// <summary>
        ///     The envelopes containing the messages that were processed.
        /// </summary>
        public IReadOnlyCollection<IRawInboundEnvelope> Envelopes { get; }
    }
}