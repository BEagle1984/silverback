// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IRawOutboundEnvelope" />
    public interface IOutboundEnvelope : IBrokerEnvelope, IRawOutboundEnvelope
    {
    }

    /// <inheritdoc />
    public interface IOutboundEnvelope<out TMessage> : IOutboundEnvelope
    {
        /// <summary>
        ///     Gets the deserialized message body.
        /// </summary>
        new TMessage Message { get; }
    }
}