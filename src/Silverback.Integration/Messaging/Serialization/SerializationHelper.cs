// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

internal static class SerializationHelper
{
    public static Type? GetTypeFromHeaders(MessageHeaderCollection messageHeaders, bool throwOnError = true) =>
        TypesCache.GetType(messageHeaders.GetValue(DefaultMessageHeaders.MessageType), throwOnError);

    public static Type GetTypeFromHeaders(MessageHeaderCollection messageHeaders, Type baseType, bool throwOnError = true)
    {
        Type? type = TypesCache.GetType(messageHeaders.GetValue(DefaultMessageHeaders.MessageType), throwOnError);
        return type == null || type.IsAssignableFrom(baseType) ? baseType : type;
    }

    public static IInboundEnvelope CreateTypedInboundEnvelope(
        IRawInboundEnvelope rawInboundEnvelope,
        object? deserializedMessage,
        Type messageType) =>
        (InboundEnvelope)Activator.CreateInstance(
            typeof(InboundEnvelope<>).MakeGenericType(messageType),
            rawInboundEnvelope,
            deserializedMessage)!;
}
