// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    internal static class SerializationHelper
    {
        public static Type GetTypeFromHeaders<TDefault>(MessageHeaderCollection messageHeaders)
        {
            var typeName = messageHeaders.GetValue(DefaultMessageHeaders.MessageType);
            return typeName != null ? Type.GetType(typeName) : typeof(TDefault);
        }

        public static IRawInboundEnvelope CreateTypedInboundEnvelope(
            IRawInboundEnvelope rawInboundEnvelope,
            object deserializedMessage)
        {
            var typedInboundMessage = (InboundEnvelope) Activator.CreateInstance(
                typeof(InboundEnvelope<>).MakeGenericType(deserializedMessage.GetType()),
                rawInboundEnvelope);

            typedInboundMessage.Message = deserializedMessage;

            return typedInboundMessage;
        }
    }
}