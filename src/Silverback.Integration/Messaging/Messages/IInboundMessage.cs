// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public interface IInboundMessage : IBrokerMessage
    {
        /// <summary>
        /// Gets the source endpoint.
        /// </summary>
        new IConsumerEndpoint Endpoint { get; }

        /// <summary>
        /// Gets a boolean value indicating whether the contained Message must be extracted and
        /// published to the internal bus. (This is true, unless specifically configured otherwise
        /// to handle some special cases.)
        /// </summary>
        bool MustUnwrap { get; }
    }

    public interface IInboundMessage<out TContent> : IInboundMessage
    {
        /// <summary>
        /// Gets the deserialized message body.
        /// </summary>
        new TContent Content { get;  }
    }
}