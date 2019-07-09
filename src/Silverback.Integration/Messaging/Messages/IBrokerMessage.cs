// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    public interface IBrokerMessage
    {
        /// <summary>
        /// Gets the optional message headers.
        /// </summary>
        MessageHeaderCollection Headers { get; }

        /// <summary>
        /// Gets the message offset (or similar construct if using a message broker other than Kafka).
        /// </summary>
        IOffset Offset { get; }

        /// <summary>
        /// Gets the source endpoint.
        /// </summary>
        IEndpoint Endpoint { get; }

        /// <summary>
        /// Gets the deserialized message body.
        /// </summary>
        object Content { get; }

        /// <summary>
        /// Gets the serialized message body.
        /// </summary>
        byte[] RawContent { get; }
    }
}