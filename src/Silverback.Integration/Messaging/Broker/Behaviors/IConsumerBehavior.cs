// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     Can be used to build a custom pipeline, plugging some functionality into the
    ///     <see cref="IConsumer" />.
    /// </summary>
    public interface IConsumerBehavior : IBrokerBehavior
    {
        /// <summary>
        ///     Process, handles or transforms the message being consumed.
        /// </summary>
        /// <param name="envelopes">
        ///     The envelopes containing the messages being consumed. It usually contains a single message
        ///     unless batch consuming is enabled.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services
        ///     in the current pipeline.
        /// </param>
        /// <param name="consumer">
        ///     The <see cref="IConsumer" /> instance that is invoking this behavior.
        /// </param>
        /// <param name="next">
        ///     The next behavior in the pipeline.
        /// </param>
        Task Handle(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            IServiceProvider serviceProvider,
            IConsumer consumer,
            RawInboundEnvelopeHandler next);
    }
}