// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    internal static class SerializationHelper
    {
        private static readonly ConcurrentDictionary<string, Type>
            TypesCache = new ConcurrentDictionary<string, Type>();

        public static Type GetTypeFromHeaders(MessageHeaderCollection messageHeaders)
        {
            var typeName = messageHeaders.GetValue(DefaultMessageHeaders.MessageType);
            if (string.IsNullOrEmpty(typeName))
                throw new MessageSerializerException("Missing type header.");

            return GetType(typeName);
        }

        public static Type GetTypeFromHeaders<TDefault>(MessageHeaderCollection messageHeaders)
        {
            var typeName = messageHeaders.GetValue(DefaultMessageHeaders.MessageType);
            return !string.IsNullOrEmpty(typeName) ? GetType(typeName) : typeof(TDefault);
        }

        public static IRawInboundEnvelope CreateTypedInboundEnvelope(
            IRawInboundEnvelope rawInboundEnvelope,
            object deserializedMessage)
        {
            var typedInboundMessage = (InboundEnvelope)Activator.CreateInstance(
                typeof(InboundEnvelope<>).MakeGenericType(deserializedMessage.GetType()),
                rawInboundEnvelope);

            typedInboundMessage.Message = deserializedMessage;

            return typedInboundMessage;
        }

        [SuppressMessage("ReSharper", "CA1031", Justification = "Can catch all, the operation is retried")]
        private static Type GetType(string typeName) =>
            TypesCache.GetOrAdd(
                typeName,
                _ =>
                {
                    Type? type = null;

                    try
                    {
                        type = Type.GetType(typeName);
                    }
                    catch
                    {
                        // Ignore
                    }

                    type ??= Type.GetType(CleanAssemblyQualifiedName(typeName), true);

                    return type;
                });

        private static string CleanAssemblyQualifiedName(string typeAssemblyQualifiedName)
        {
            if (string.IsNullOrEmpty(typeAssemblyQualifiedName))
                return typeAssemblyQualifiedName;

            var split = typeAssemblyQualifiedName.Split(',');

            return split.Length >= 2 ? $"{split[0]}, {split[1]}" : typeAssemblyQualifiedName;
        }
    }
}
