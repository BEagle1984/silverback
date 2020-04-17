// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Messages
{
    public abstract class ConsumingEvent : ISilverbackEvent
    {
        protected ConsumingEvent(ConsumerPipelineContext context)
        {
            Context = context;
        }

        /// <summary>
        ///     The instance of <see cref="ConsumerPipelineContext" /> this event refers to.
        /// </summary>
        public ConsumerPipelineContext Context { get; }
    }
}