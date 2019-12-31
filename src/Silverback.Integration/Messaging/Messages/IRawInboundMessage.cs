// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public interface IRawInboundMessage
    {
        /// <summary>
        ///     Gets the source endpoint.
        /// </summary>
        IConsumerEndpoint Endpoint { get; }
    }
}