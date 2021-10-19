// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Wraps the serialized inbound or outbound message.
    /// </summary>
    public interface IRawBrokerEnvelope
    {
        /// <summary>
        ///     Gets the optional message headers.
        /// </summary>
        MessageHeaderCollection Headers { get; }

        /// <summary>
        ///     Gets the the source or destination endpoint.
        /// </summary>
        Endpoint Endpoint { get; }

        /// <summary>
        ///     Gets or sets the serialized message body.
        /// </summary>
        Stream? RawMessage { get; set; }
    }
}
