// TODO: DELETE

//// Copyright (c) 2018-2019 Sergio Aquilini
//// This code is licensed under MIT license (see LICENSE file for details)

//using System;

//namespace Silverback.Messaging.Messages
//{
//    internal static class InboundMessageHelper
//    {
//        public static IInboundMessage CreateInboundMessage(object deserializedMessage, IInboundMessage rawInboundMessage) => 
//            CreateInboundMessage<object>(deserializedMessage, rawInboundMessage);

//        public static IInboundMessage<TMessage> CreateInboundMessage<TMessage>(IInboundMessage rawInboundMessage)
//        {


//            if (rawInboundMessage.Headers != null)
//                newMessage.Headers.AddRange(rawInboundMessage.Headers);

//            return (IInboundMessage<TMessage>) newMessage;
//        }
//    }
//}