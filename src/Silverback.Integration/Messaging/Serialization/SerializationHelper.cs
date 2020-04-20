// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    internal static class SerializationHelper
    {
        public static readonly ConcurrentDictionary<string, Type> TypesCache = new ConcurrentDictionary<string, Type>();
        
        public static Type GetTypeFromHeaders<TDefault>(MessageHeaderCollection messageHeaders)
        {
            var typeName = messageHeaders.GetValue(DefaultMessageHeaders.MessageType);
            if (string.IsNullOrEmpty(typeName))
                return typeof(TDefault);

            return TypesCache.GetOrAdd(typeName, _ =>
            {
                Type type = null;
                try
                {
                    type = Type.GetType(typeName);
                }
                catch (Exception)
                {
                    // Ignore
                }
                
                type ??= Type.GetType(CleanAssemblyQualifiedName(typeName));

                return type;
            });
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

        private static string CleanAssemblyQualifiedName(string typeAssemblyQualifiedName)
        {
            if (string.IsNullOrEmpty(typeAssemblyQualifiedName))
                return typeAssemblyQualifiedName;

            var split = typeAssemblyQualifiedName.Split(',');

            return split.Length >= 2 ? $"{split[0]}, {split[1]}" : typeAssemblyQualifiedName;
        }
    }
}