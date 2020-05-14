// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Fired when an exception occurs while processing a consumed message.
    /// </summary>
    public class ConsumingAbortedEvent : ConsumingEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsumingAbortedEvent"/> class.
        /// </summary>
        /// <param name="context">
        ///     The context that is passed along the consumer behaviors pipeline.
        /// </param>
        /// <param name="exception">
        ///    The <see cref="Exception" /> that was thrown while trying to process the messages.
        /// </param>
        public ConsumingAbortedEvent(ConsumerPipelineContext context, Exception exception)
            : base(context)
        {
            Exception = exception;
        }

        /// <summary>
        ///     Gets the <see cref="Exception" /> that was thrown while trying to process the messages.
        /// </summary>
        public Exception Exception { get; }
    }
}