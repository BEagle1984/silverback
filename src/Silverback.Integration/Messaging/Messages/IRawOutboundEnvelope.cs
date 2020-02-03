// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public interface IRawOutboundEnvelope : IRawBrokerEnvelope
    {
        /// <summary>
        ///     Gets the destination endpoint.
        /// </summary>
        new IProducerEndpoint Endpoint { get; }
    }
}