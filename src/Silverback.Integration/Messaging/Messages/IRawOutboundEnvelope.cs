// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Wraps the message that is being routed to an outbound endpoint.
    /// </summary>
    public interface IRawOutboundEnvelope : IRawBrokerEnvelope
    {
        /// <summary>
        ///     Gets the destination endpoint.
        /// </summary>
        new IProducerEndpoint Endpoint { get; }
    }
}