// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     Connects to the specified schema registry and serializes the messages in Apache Avro format.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be serialized and/or deserialized.
    /// </typeparam>
    public class AvroMessageDeserializer<TMessage> : AvroMessageDeserializerBase
        where TMessage : class
    {
        /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
        public override ValueTask<Stream?> SerializeAsync(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        /// <inheritdoc cref="IMessageSerializer.DeserializeAsync" />
        public override async ValueTask<(object? Message, Type MessageType)> DeserializeAsync(
            Stream? messageStream,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context,
            CancellationToken cancellationToken = default)
        {
            var buffer = await messageStream.ReadAllAsync(cancellationToken).ConfigureAwait(false);
            var deserialized = await DeserializeAsync<TMessage>(buffer, MessageComponentType.Value, context)
                .ConfigureAwait(false);

            var type = deserialized?.GetType() ?? typeof(TMessage);

            return (deserialized, type);
        }

        /// <inheritdoc cref="IKafkaMessageSerializer.SerializeKey" />
        public override byte[] SerializeKey(
            string key,
            IReadOnlyCollection<MessageHeader>? messageHeaders,
            MessageSerializationContext context) =>
            throw new NotSupportedException();

        /// <inheritdoc cref="IKafkaMessageSerializer.DeserializeKey" />
        public override string DeserializeKey(
            byte[] key,
            IReadOnlyCollection<MessageHeader>? messageHeaders,
            MessageSerializationContext context)
        {
            Check.NotNull(key, nameof(key));

            return AsyncHelper.RunSynchronously(() => DeserializeAsync<string>(key, MessageComponentType.Key, context))!;
        }

        private static SerializationContext GetConfluentSerializationContext(
            MessageComponentType componentType,
            MessageSerializationContext context) =>
            new(componentType, context.ActualEndpointName);

        private async Task<TValue?> DeserializeAsync<TValue>(
            byte[]? message,
            MessageComponentType componentType,
            MessageSerializationContext context)
            where TValue : class
        {
            if (message == null)
                return null;

            var avroDeserializer = new AvroDeserializer<TValue>(
                SchemaRegistryClientFactory.GetClient(SchemaRegistryConfig),
                AvroDeserializerConfig);

            var confluentSerializationContext = GetConfluentSerializationContext(componentType, context);

            return await avroDeserializer.DeserializeAsync(
                    new ReadOnlyMemory<byte>(message),
                    false,
                    confluentSerializationContext)
                .ConfigureAwait(false);
        }
    }
}
