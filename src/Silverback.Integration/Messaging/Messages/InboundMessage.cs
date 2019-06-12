// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    internal class InboundMessage : IInboundMessage
    {
        public object Message { get; set; }

        public MessageHeaderCollection Headers { get; } = new MessageHeaderCollection();

        public IOffset Offset { get; set; }

        public IEndpoint Endpoint { get; set; }

        public int FailedAttempts
        {
            get => Headers.GetValue<int>(MessageHeader.FailedAttemptsHeaderName);
            set
            {
                Headers.Remove(MessageHeader.FailedAttemptsHeaderName);

                if (value > 0)
                    Headers.Add(MessageHeader.FailedAttemptsHeaderName, value.ToString());
            }
        }

        public bool MustUnwrap { get; set; }
    }

    internal class InboundMessage<TMessage> : InboundMessage, IInboundMessage<TMessage>
    {
        public new TMessage Message
        {
            get => (TMessage) base.Message;
            set => base.Message = value;
        }
    }
}
