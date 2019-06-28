// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages
{
    internal static class InboundMessageHelper
    {
        public static IInboundMessage CreateInboundMessage(object deserializedMessage, IRawInboundMessage rawInboundMessage) => 
            CreateInboundMessage<object>(deserializedMessage, rawInboundMessage);

        public static IInboundMessage<TMessage> CreateInboundMessage<TMessage>(TMessage deserializedMessage, IRawInboundMessage rawInboundMessage)
        {
            var newMessage = (InboundMessage) Activator.CreateInstance(
                    typeof(InboundMessage<>).MakeGenericType(deserializedMessage.GetType()),
                    deserializedMessage,
                    rawInboundMessage);

            if (rawInboundMessage.Headers != null)
                newMessage.Headers.AddRange(rawInboundMessage.Headers);

            return (IInboundMessage<TMessage>) newMessage;
        }
    }
}