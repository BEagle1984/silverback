// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The base class for the events thrown by the consumer pipeline (e.g.
    ///     <see cref="ConsumingCompletedEvent" /> or <see cref="ConsumingAbortedEvent" />).
    /// </summary>
    public abstract class ConsumingEvent : ISilverbackEvent
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumingEvent" /> class.
        /// </summary>
        /// <param name="context">
        ///     The context that is passed along the consumer behaviors pipeline.
        /// </param>
        protected ConsumingEvent(ConsumerPipelineContext context)
        {
            Context = context;
        }

        /// <summary>
        ///     Gets the context that is passed along the consumer behaviors pipeline.
        /// </summary>
        public ConsumerPipelineContext Context { get; }
    }
}
