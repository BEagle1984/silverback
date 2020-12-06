// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
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
    public class AvroMessageSerializer<TMessage> : AvroMessageSerializerBase
        where TMessage : class
    {
        /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
        public override async ValueTask<Stream?> SerializeAsync(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            var buffer = await SerializeAsync<TMessage>(message, MessageComponentType.Value, context)
                .ConfigureAwait(false);

            return buffer == null ? null : new MemoryStream(buffer);
        }

        /// <inheritdoc cref="IMessageSerializer.DeserializeAsync" />
        public override async ValueTask<(object? Message, Type MessageType)> DeserializeAsync(
            Stream? messageStream,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            var buffer = await messageStream.ReadAllAsync().ConfigureAwait(false);
            var deserialized = await DeserializeAsync<TMessage>(buffer, MessageComponentType.Value, context)
                .ConfigureAwait(false);

            var type = deserialized?.GetType() ?? typeof(TMessage);

            return (deserialized, type);
        }

        /// <inheritdoc cref="IKafkaMessageSerializer.SerializeKey" />
        public override byte[] SerializeKey(
            string key,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            Check.NotEmpty(key, nameof(key));

            byte[]? serializedKey = AsyncHelper.RunSynchronously(
                () => SerializeAsync<string>(key, MessageComponentType.Key, context));

            return serializedKey ?? Array.Empty<byte>();
        }

        /// <inheritdoc cref="IKafkaMessageSerializer.DeserializeKey" />
        public override string DeserializeKey(
            byte[] key,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            Check.NotNull(key, nameof(key));

            return AsyncHelper.RunSynchronously(
                () => DeserializeAsync<string>(key, MessageComponentType.Key, context))!;
        }

        private static SerializationContext GetConfluentSerializationContext(
            MessageComponentType componentType,
            MessageSerializationContext context) =>
            new(componentType, context.ActualEndpointName);

        private async ValueTask<byte[]?> SerializeAsync<TValue>(
            object? message,
            MessageComponentType componentType,
            MessageSerializationContext context)
        {
            if (message == null)
                return null;

            if (message is Stream inputStream)
                return await inputStream.ReadAllAsync().ConfigureAwait(false);

            if (message is byte[] inputBytes)
                return inputBytes;

            return await new AvroSerializer<TValue>(
                    SchemaRegistryClientFactory.GetClient(SchemaRegistryConfig),
                    AvroSerializerConfig)
                .SerializeAsync(
                    (TValue)message,
                    GetConfluentSerializationContext(componentType, context))
                .ConfigureAwait(false);
        }

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
                AvroSerializerConfig);

            var confluentSerializationContext = GetConfluentSerializationContext(componentType, context);

            return await avroDeserializer.DeserializeAsync(
                    new ReadOnlyMemory<byte>(message),
                    false,
                    confluentSerializationContext)
                .ConfigureAwait(false);
        }
    }
}
