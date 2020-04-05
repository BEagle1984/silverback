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
        public ConsumingAbortedEvent(IReadOnlyCollection<IRawInboundEnvelope> envelopes, Exception exception)
            : base(envelopes)
        {
            Exception = exception;
        }

        /// <summary>
        ///     Gets the <see cref="Exception" /> that was thrown while trying to process the messages.
        /// </summary>
        public Exception Exception { get; }
    }
}