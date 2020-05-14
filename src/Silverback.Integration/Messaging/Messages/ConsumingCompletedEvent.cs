// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Fired when the processing of a consumed message is successfully completed.
    /// </summary>
    public class ConsumingCompletedEvent : ConsumingEvent
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumingCompletedEvent" /> class.
        /// </summary>
        /// <param name="context">
        ///     The context that is passed along the consumer behaviors pipeline.
        /// </param>
        public ConsumingCompletedEvent(ConsumerPipelineContext context)
            : base(context)
        {
        }
    }
}
