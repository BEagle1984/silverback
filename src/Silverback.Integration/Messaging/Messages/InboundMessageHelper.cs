// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages
{
    internal static class InboundMessageHelper
    {
        public static IInboundMessage CreateNewInboundMessage(object message, IInboundMessage sourceInboundMessage) => 
            CreateNewInboundMessage<object>(message, sourceInboundMessage);

        public static IInboundMessage<TMessage> CreateNewInboundMessage<TMessage>(TMessage message,
            IInboundMessage sourceInboundMessage)
        {
            var newMessage = (InboundMessage) Activator.CreateInstance(typeof(InboundMessage<>).MakeGenericType(message.GetType()));

            newMessage.Message = message;
            newMessage.Endpoint = sourceInboundMessage.Endpoint;
            newMessage.Offset = sourceInboundMessage.Offset;
            newMessage.FailedAttempts = sourceInboundMessage.FailedAttempts;
            newMessage.MustUnwrap = sourceInboundMessage.MustUnwrap;

            if (sourceInboundMessage.Headers != null)
                newMessage.Headers.AddRange(sourceInboundMessage.Headers);

            return (IInboundMessage<TMessage>) newMessage;
        }
    }
}