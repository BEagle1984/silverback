// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

internal static class SerializationHelper
{
    public static Type? GetTypeFromHeaders(MessageHeaderCollection messageHeaders, bool throwOnError = true)
    {
        string? typeName = messageHeaders.GetValue(DefaultMessageHeaders.MessageType);

        if (string.IsNullOrEmpty(typeName))
            return null;

        return TypesCache.GetType(typeName, throwOnError);
    }

    public static IRawInboundEnvelope CreateTypedInboundEnvelope(
        IRawInboundEnvelope rawInboundEnvelope,
        object? deserializedMessage,
        Type messageType) =>
        (InboundEnvelope)Activator.CreateInstance(
            typeof(InboundEnvelope<>).MakeGenericType(messageType),
            rawInboundEnvelope,
            deserializedMessage)!;
}
