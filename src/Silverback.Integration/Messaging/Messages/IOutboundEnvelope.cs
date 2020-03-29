// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public interface IOutboundEnvelope : IBrokerEnvelope, IRawOutboundEnvelope
    {
        /// <summary>
        ///     Gets the destination endpoint.
        /// </summary>
        new IProducerEndpoint Endpoint { get; }
    }

    public interface IOutboundEnvelope<out TMessage> : IOutboundEnvelope
    {
        /// <summary>
        ///     Gets the deserialized message body.
        /// </summary>
        new TMessage Message { get; }
    }
}