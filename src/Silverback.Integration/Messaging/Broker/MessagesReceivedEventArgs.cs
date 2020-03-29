// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class MessagesReceivedEventArgs : EventArgs
    {
        public MessagesReceivedEventArgs(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            IServiceProvider serviceProvider)
        {
            Envelopes = envelopes;
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        ///     Gets the <see cref="IRawInboundEnvelope" /> instances (or <see cref="IInboundEnvelope" /> when properly
        ///     deserialized) that contain the messages that have been consumed are ready to be processed.
        /// </summary>
        public IReadOnlyCollection<IRawInboundEnvelope> Envelopes { get; }

        /// <summary>
        ///     Gets the instance of <see cref="IServiceProvider" /> scoped to the processing of the current message.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }
    }
}