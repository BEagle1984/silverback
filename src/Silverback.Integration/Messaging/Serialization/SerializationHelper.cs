// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization
{
    internal static class SerializationHelper
    {
        public static Type GetTypeFromHeaders(MessageHeaderCollection messageHeaders)
        {
            var typeName = messageHeaders.GetValue(DefaultMessageHeaders.MessageType);
            if (string.IsNullOrEmpty(typeName))
                throw new MessageSerializerException("Missing type header.");

            return TypesCache.GetType(typeName);
        }

        public static Type GetTypeFromHeaders<TDefault>(MessageHeaderCollection messageHeaders)
        {
            var typeName = messageHeaders.GetValue(DefaultMessageHeaders.MessageType);
            return !string.IsNullOrEmpty(typeName) ? TypesCache.GetType(typeName) : typeof(TDefault);
        }

        public static IRawInboundEnvelope CreateTypedInboundEnvelope(
            IRawInboundEnvelope rawInboundEnvelope,
            object? deserializedMessage,
            Type messageType)
        {
            var typedInboundMessage = (InboundEnvelope)Activator.CreateInstance(
                typeof(InboundEnvelope<>).MakeGenericType(messageType),
                rawInboundEnvelope,
                deserializedMessage);

            return typedInboundMessage;
        }
    }
}
