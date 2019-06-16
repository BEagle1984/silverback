// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    public interface IInboundMessage
    {
        /// <summary>
        /// Gets the message.
        /// </summary>
        object Message { get; }

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
        /// Gets the number of failed processing attempt for this message.
        /// </summary>
        int FailedAttempts { get; }

        /// <summary>
        /// Gets a boolean value indicating whether the contained Message must be extracted and
        /// published to the internal bus. (This is true, unless specifically configured otherwise
        /// to handle some special cases.)
        /// </summary>
        bool MustUnwrap { get; }
    }

    public interface IInboundMessage<out TMessage> : IInboundMessage
    {
        /// <summary>
        /// Gets the deserialized message.
        /// </summary>
        new TMessage Message { get; }
    }
}