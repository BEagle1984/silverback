// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
    /// <inheritdoc cref="IKafkaMessageSerializer" />
    public class AvroMessageSerializer<TMessage> : IKafkaMessageSerializer
    {
        /// <summary>
        ///     Gets or sets the schema registry configuration.
        /// </summary>
        public SchemaRegistryConfig SchemaRegistryConfig { get; set; } = new SchemaRegistryConfig();

        /// <summary>
        ///     Gets or sets the Avro serializer configuration.
        /// </summary>
        public AvroSerializerConfig AvroSerializerConfig { get; set; } = new AvroSerializerConfig();

        /// <summary>
        ///     Gets or sets
        /// </summary>
        public IEndpoint Endpoint { get; set; }

        public byte[] Serialize(
            object message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            AsyncHelper.RunSynchronously(() => SerializeAsync(message, messageHeaders, context));

        public object Deserialize(
            byte[] message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            AsyncHelper.RunSynchronously(() => DeserializeAsync(message, messageHeaders, context));

        public async Task<byte[]> SerializeAsync(
            object message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            await SerializeAsync<TMessage>(message, messageHeaders, MessageComponentType.Value, context);

        public async Task<object> DeserializeAsync(
            byte[] message,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            await DeserializeAsync<TMessage>(message, messageHeaders, MessageComponentType.Value, context);

        public byte[] SerializeKey(
            string key,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            AsyncHelper.RunSynchronously(() =>
                SerializeAsync<string>(key, messageHeaders, MessageComponentType.Key, context));

        public string DeserializeKey(
            byte[] key,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context) =>
            AsyncHelper.RunSynchronously(() =>
                DeserializeAsync<string>(key, messageHeaders, MessageComponentType.Key, context));

        private async Task<byte[]> SerializeAsync<TValue>(
            object message,
            MessageHeaderCollection messageHeaders,
            MessageComponentType componentType,
            MessageSerializationContext context)
        {
            if (messageHeaders == null) throw new ArgumentNullException(nameof(messageHeaders));

            switch (message)
            {
                case null:
                    return new byte[0];
                case byte[] bytes:
                    return bytes;
            }

            return await new AvroSerializer<TValue>(
                    SchemaRegistryClientFactory.GetClient(SchemaRegistryConfig), AvroSerializerConfig)
                .SerializeAsync(
                    (TValue) message,
                    GetConfluentSerializationContext(componentType, context));
        }

        private async Task<TValue> DeserializeAsync<TValue>(
            byte[] message,
            MessageHeaderCollection messageHeaders,
            MessageComponentType componentType,
            MessageSerializationContext context)
        {
            if (messageHeaders == null) throw new ArgumentNullException(nameof(messageHeaders));

            if (message == null || message.Length == 0)
                return default;

            return await new AvroDeserializer<TValue>(
                    SchemaRegistryClientFactory.GetClient(SchemaRegistryConfig), AvroSerializerConfig)
                .DeserializeAsync(
                    new ReadOnlyMemory<byte>(message),
                    false,
                    GetConfluentSerializationContext(componentType, context));
        }

        private SerializationContext GetConfluentSerializationContext(
            MessageComponentType componentType,
            MessageSerializationContext context) =>
            new SerializationContext(componentType, context.ActualEndpointName);
    }
}