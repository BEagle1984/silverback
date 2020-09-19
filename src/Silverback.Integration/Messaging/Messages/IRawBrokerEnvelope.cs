// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;

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
        MessageHeaderCollection Headers { get; } // TODO: Can/must be readonly? Some sort of IReadOnlyHeaderCollection?

        /// <summary>
        ///     Gets the message offset (or similar construct if using a message broker other than Kafka).
        /// </summary>
        IOffset? Offset { get; }

        /// <summary>
        ///     Gets the source or destination endpoint.
        /// </summary>
        IEndpoint Endpoint { get; }

        /// <summary>
        ///     Gets a dictionary containing some additional (usually broker specific) data to be logged together with
        ///     the standard data such as endpoint name, message type, etc. Some examples of such data are the Kafka
        ///     key or the Rabbit routing key.
        /// </summary>
        IDictionary<string, string> AdditionalLogData { get; }

        /// <summary>
        ///     Gets or sets the serialized message body.
        /// </summary>
        Stream? RawMessage { get; set; } // TODO: Should this be get only? Or all the rest should be get/set as well?
    }
}
