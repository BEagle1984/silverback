// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public interface IInboundEnvelope : IBrokerEnvelope, IRawInboundEnvelope
    {
        /// <summary>
        ///     Gets the source endpoint configuration.
        /// </summary>
        new IConsumerEndpoint Endpoint { get; }
    }

    public interface IInboundEnvelope<out TMessage> : IInboundEnvelope
    {
        /// <summary>
        ///     Gets the deserialized message body.
        /// </summary>
        new TMessage Message { get; }
    }
}