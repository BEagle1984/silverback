// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public interface IRawOutboundMessage
    {
        /// <summary>
        ///     Gets the destination endpoint.
        /// </summary>
        IProducerEndpoint Endpoint { get; }
    }
}