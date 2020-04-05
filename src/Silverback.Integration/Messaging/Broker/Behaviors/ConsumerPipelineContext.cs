// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     The context that is passed along the consumer behaviors pipeline.
    /// </summary>
    public class ConsumerPipelineContext
    {
        public ConsumerPipelineContext(IReadOnlyCollection<IRawInboundEnvelope> envelopes, IConsumer consumer)
        {
            Envelopes = envelopes;
            Consumer = consumer;
        }

        /// <summary>
        ///     Gets or sets the envelopes containing the messages being consumed.
        /// </summary>
        public IReadOnlyCollection<IRawInboundEnvelope> Envelopes { get; set; }

        /// <summary>
        ///     Gets the instance of <see cref="IConsumer" /> that triggered this pipeline.
        /// </summary>
        public IConsumer Consumer { get; }
    }
}