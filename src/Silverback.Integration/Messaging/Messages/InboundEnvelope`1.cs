// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IInboundEnvelope{TMessage}" />
    internal class InboundEnvelope<TMessage> : InboundEnvelope, IInboundEnvelope<TMessage>
        where TMessage : class
    {
        public InboundEnvelope(IRawInboundEnvelope envelope, TMessage message)
            : base(envelope)
        {
            Message = message;
        }

        public new TMessage? Message
        {
            get => (TMessage?)base.Message;
            set => base.Message = value;
        }
    }
}
