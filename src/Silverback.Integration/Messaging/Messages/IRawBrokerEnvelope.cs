// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
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
        MessageHeaderCollection Headers { get; }

        /// <summary>
        ///     Gets the message offset (or similar construct if using a message broker other than Kafka).
        /// </summary>
        IOffset Offset { get; }

        /// <summary>
        ///     Gets the source or destination endpoint.
        /// </summary>
        IEndpoint Endpoint { get; }

        /// <summary>
        ///     Gets or sets the serialized message body.
        /// </summary>
        [SuppressMessage("ReSharper", "CA1819", Justification = Justifications.CanExposeByteArray)]
        [SuppressMessage("ReSharper", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        byte[]? RawMessage { get; set; }
    }
}