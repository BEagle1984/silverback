// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     The argument passed to the <see cref="MessagesReceivedCallback" /> containing the received messages
    ///     and the scoped service provider.
    /// </summary>
    public class MessagesReceivedCallbackArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MessagesReceivedCallbackArgs" /> class.
        /// </summary>
        /// <param name="envelopes">
        ///     The envelopes containing the messages that have been consumed.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> scoped to the processing of the current messages.
        /// </param>
        /// <param name="consumer">
        ///     The <see cref="IConsumer" /> that consumed the messages.
        /// </param>
        public MessagesReceivedCallbackArgs(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            IServiceProvider serviceProvider,
            IConsumer consumer)
        {
            Envelopes = envelopes;
            ServiceProvider = serviceProvider;
            Consumer = consumer;
        }

        /// <summary>
        ///     Gets the collection of <see cref="IRawInboundEnvelope" /> (or <see cref="IInboundEnvelope" /> when
        ///     properly deserialized) that contain the messages that have been consumed and are ready to be
        ///     processed.
        /// </summary>
        public IReadOnlyCollection<IRawInboundEnvelope> Envelopes { get; }

        /// <summary>
        ///     Gets the <see cref="IServiceProvider" /> scoped to the processing of the current messages.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        ///     Gets the <see cref="IConsumer" /> that consumed the messages.
        /// </summary>
        public IConsumer Consumer { get; }
    }
}
