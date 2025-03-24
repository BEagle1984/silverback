// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

internal static class SerializationHelper
{
    private static readonly ConcurrentDictionary<Type, ConstructorInfo> Constructors = new();

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
        Type messageType)
    {
        ConstructorInfo constructor = Constructors.GetOrAdd(
            messageType,
            type => typeof(InboundEnvelope<>).MakeGenericType(type).GetConstructor([typeof(IRawInboundEnvelope), type])!);

        return constructor.Invoke([rawInboundEnvelope, deserializedMessage]) as IInboundEnvelope
               ?? throw new InvalidOperationException("Failed to create the typed envelope.");
    }
}
