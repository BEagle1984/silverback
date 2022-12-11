// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IInboundEnvelope{TMessage}" />
internal sealed record InboundEnvelope<TMessage> : InboundEnvelope, IInboundEnvelope<TMessage>
    where TMessage : class
{
    public InboundEnvelope(IRawInboundEnvelope envelope, TMessage? message)
        : base(envelope, message)
    {
        Message = message;
    }

    public new TMessage? Message { get; }
}
