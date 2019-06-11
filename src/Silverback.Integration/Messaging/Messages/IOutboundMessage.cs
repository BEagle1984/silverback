// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)
namespace Silverback.Messaging.Messages
{
    public interface IOutboundMessage
    {
        /// <summary>
        /// Gets the destination endpoint.
        /// </summary>
        IEndpoint Endpoint { get; set; }

        /// <summary>
        /// Gets the optional message headers.
        /// </summary>
        MessageHeaderCollection Headers { get; }

        /// <summary>
        /// Gets the message.
        /// </summary>
        object Message { get; set; }
    }

    public interface IOutboundMessage<out TMessage> : IOutboundMessage
    {
        new TMessage Message { get; }
    }
}