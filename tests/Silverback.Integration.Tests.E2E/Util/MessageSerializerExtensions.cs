// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Silverback.Collections;
using Silverback.Messaging;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Validation;
using Silverback.Util;

namespace Silverback.Tests.Integration.E2E.Util
{
    public static class MessageSerializerExtensions
    {
        public static ValueTask<Stream?> SerializeAsync(
            this IMessageSerializer serializer,
            object message,
            MessageHeaderCollection? headers = null) =>
            serializer.SerializeAsync(message, headers ?? new MessageHeaderCollection(), NullProducerEndpoint.Instance);

        public static Stream Serialize(
            this IMessageSerializer serializer,
            object message,
            MessageHeaderCollection? headers = null) =>
            SerializeAsync(serializer, message, headers).AsTask().Result ??
            throw new InvalidOperationException("Serializer returned null");

        [SuppressMessage("Usage", "VSTHRD104:Offer async methods", Justification = "Test method")]
        public static byte[] SerializeToBytes(
            this IMessageSerializer serializer,
            object message,
            MessageHeaderCollection? headers = null) =>
            Serialize(serializer, message, headers).ReadAll() ?? throw new InvalidOperationException("Serializer returned null");

        private record NullProducerEndpoint : ProducerEndpoint
        {
            private NullProducerEndpoint()
                : base("null", new NullProducerConfiguration())
            {
            }

            public static NullProducerEndpoint Instance { get; } = new();

            private record NullProducerConfiguration : ProducerConfiguration<NullProducerEndpoint>;
        }
    }
}
