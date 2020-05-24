// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
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
    public class AvroMessageSerializer<TMessage> : IKafkaMessageSerializer
        where TMessage : class
    {
        /// <summary> Gets or sets the schema registry configuration. </summary>
        public SchemaRegistryConfig SchemaRegistryConfig { get; set; } = new SchemaRegistryConfig();

        /// <summary> Gets or sets the Avro serializer configuration. </summary>
        public AvroSerializerConfig AvroSerializerConfig { get; set; } = new AvroSerializerConfig();

        /// <inheritdoc />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public byte[]? Serialize(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            AsyncHelper.RunSynchronously(() => SerializeAsync(message, messageHeaders, context));

        /// <inheritdoc />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public (object?, Type) Deserialize(
            byte[]? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            AsyncHelper.RunSynchronously(() => DeserializeAsync(message, messageHeaders, context));

        /// <inheritdoc />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task<byte[]?> SerializeAsync(
            object? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            await SerializeAsync<TMessage>(message, MessageComponentType.Value, context);

        /// <inheritdoc />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task<(object?, Type)> DeserializeAsync(
            byte[]? message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            var deserialized = await DeserializeAsync<TMessage>(message, MessageComponentType.Value, context);
            var type = deserialized?.GetType() ?? typeof(TMessage);

            return (deserialized, type);
        }

        /// <inheritdoc />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public byte[] SerializeKey(
            string key,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context)
        {
            Check.NotEmpty(key, nameof(key));

            byte[]? serializedKey = AsyncHelper.RunSynchronously(
                () => SerializeAsync<string>(key, MessageComponentType.Key, context));

            return serializedKey ?? Array.Empty<byte>();
        }

        /// <inheritdoc />
        [SuppressMessage("", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public string DeserializeKey(
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
            new SerializationContext(componentType, context.ActualEndpointName);

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private async Task<byte[]?> SerializeAsync<TValue>(
            object? message,
            MessageComponentType componentType,
            MessageSerializationContext context)
        {
            if (message == null)
                return null;

            if (message is byte[] bytes)
                return bytes;

            return await new AvroSerializer<TValue>(
                    SchemaRegistryClientFactory.GetClient(SchemaRegistryConfig),
                    AvroSerializerConfig)
                .SerializeAsync(
                    (TValue)message,
                    GetConfluentSerializationContext(componentType, context));
        }

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private async Task<TValue?> DeserializeAsync<TValue>(
            byte[]? message,
            MessageComponentType componentType,
            MessageSerializationContext context)
            where TValue : class
        {
            if (message == null || message.Length == 0)
                return null;

            var avroDeserializer = new AvroDeserializer<TValue>(
                SchemaRegistryClientFactory.GetClient(SchemaRegistryConfig),
                AvroSerializerConfig);

            var confluentSerializationContext = GetConfluentSerializationContext(componentType, context);

            return await avroDeserializer.DeserializeAsync(
                new ReadOnlyMemory<byte>(message),
                false,
                confluentSerializationContext);
        }
    }
}
