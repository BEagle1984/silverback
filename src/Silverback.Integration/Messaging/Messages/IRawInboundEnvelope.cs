// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public interface IRawInboundEnvelope : IRawBrokerEnvelope
    {
        /// <summary>
        ///     Gets the source endpoint.
        /// </summary>
        new IConsumerEndpoint Endpoint { get; }
    }
}