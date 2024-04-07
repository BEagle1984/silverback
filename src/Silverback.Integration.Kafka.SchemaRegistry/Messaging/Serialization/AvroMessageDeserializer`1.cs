// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Connects to the specified schema registry and deserializes the messages from Apache Avro format.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be deserialized.
/// </typeparam>
public class AvroMessageDeserializer<TMessage> : IAvroMessageDeserializer
    where TMessage : class
{
    /// <inheritdoc cref="IMessageDeserializer.RequireHeaders" />
    public bool RequireHeaders => false;

    /// <inheritdoc cref="IAvroMessageDeserializer.SchemaRegistryConfig" />
    public SchemaRegistryConfig SchemaRegistryConfig { get; set; } = new();

    /// <inheritdoc cref="IAvroMessageDeserializer.AvroDeserializerConfig" />
    public AvroDeserializerConfig AvroDeserializerConfig { get; set; } = new();

    /// <inheritdoc cref="IMessageDeserializer.DeserializeAsync" />
    public async ValueTask<DeserializedMessage> DeserializeAsync(
        Stream? messageStream,
        MessageHeaderCollection headers,
        ConsumerEndpoint endpoint)
    {
        byte[]? buffer = await messageStream.ReadAllAsync().ConfigureAwait(false);
        TMessage? deserialized = await DeserializeAsync<TMessage>(buffer, MessageComponentType.Value, endpoint)
            .ConfigureAwait(false);

        Type type = deserialized?.GetType() ?? typeof(TMessage);

        return new DeserializedMessage(deserialized, type);
    }

    /// <inheritdoc cref="IMessageDeserializer.GetCompatibleSerializer" />
    public IMessageSerializer GetCompatibleSerializer() => new AvroMessageSerializer<TMessage>
    {
        SchemaRegistryConfig = SchemaRegistryConfig,
        AvroSerializerConfig = new AvroSerializerConfig(AvroDeserializerConfig)
    };

    /// <inheritdoc cref="IKafkaMessageDeserializer.DeserializeKey" />
    public string DeserializeKey(byte[] key, IReadOnlyCollection<MessageHeader>? headers, KafkaConsumerEndpoint endpoint)
    {
        Check.NotNull(key, nameof(key));

        return DeserializeAsync<string>(key, MessageComponentType.Key, endpoint).SafeWait() ??
               throw new InvalidOperationException("The key could not be deserialized.");
    }

    private static SerializationContext GetConfluentSerializationContext(MessageComponentType componentType, Endpoint endpoint) =>
        new(componentType, endpoint.RawName);

    private async Task<TValue?> DeserializeAsync<TValue>(byte[]? message, MessageComponentType componentType, ConsumerEndpoint endpoint)
        where TValue : class
    {
        if (message == null)
            return null;

        AvroDeserializer<TValue> avroDeserializer = new(SchemaRegistryClientFactory.GetClient(SchemaRegistryConfig), AvroDeserializerConfig);

        SerializationContext confluentSerializationContext = GetConfluentSerializationContext(componentType, endpoint);

        return await avroDeserializer.DeserializeAsync(new ReadOnlyMemory<byte>(message), false, confluentSerializationContext)
            .ConfigureAwait(false);
    }
}
