// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IInboundEnvelope{TMessage}" />
internal sealed class InboundEnvelope<TMessage> : InboundEnvelope, IInboundEnvelope<TMessage>
    where TMessage : class
{
    private TMessage? _message;

    public InboundEnvelope(IRawInboundEnvelope envelope, TMessage message)
        : base(envelope)
    {
        Message = message;
    }

    public new TMessage? Message
    {
        get => _message;
        set => base.Message = _message = value;
    }
}
