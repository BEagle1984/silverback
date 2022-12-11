// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

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
    public override async ValueTask<Stream?> SerializeAsync(object? message, MessageHeaderCollection headers, ProducerEndpoint endpoint)
    {
        byte[]? buffer = await SerializeAsync<TMessage>(message, MessageComponentType.Value, endpoint)
            .ConfigureAwait(false);

        return buffer == null ? null : new MemoryStream(buffer);
    }

    /// <inheritdoc cref="IMessageSerializer.DeserializeAsync" />
    public override async ValueTask<DeserializedMessage> DeserializeAsync(
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

    /// <inheritdoc cref="IKafkaMessageSerializer.SerializeKey" />
    public override byte[] SerializeKey(
        string key,
        IReadOnlyCollection<MessageHeader>? headers,
        KafkaProducerEndpoint endpoint)
    {
        Check.NotNullOrEmpty(key, nameof(key));

        byte[]? serializedKey = AsyncHelper.RunSynchronously(() => SerializeAsync<string>(key, MessageComponentType.Key, endpoint).AsTask());

        return serializedKey ?? Array.Empty<byte>();
    }

    /// <inheritdoc cref="IKafkaMessageSerializer.DeserializeKey" />
    public override string DeserializeKey(
        byte[] key,
        IReadOnlyCollection<MessageHeader>? headers,
        KafkaConsumerEndpoint endpoint)
    {
        Check.NotNull(key, nameof(key));

        return AsyncHelper.RunSynchronously(() => DeserializeAsync<string>(key, MessageComponentType.Key, endpoint))!;
    }

    private static SerializationContext GetConfluentSerializationContext(
        MessageComponentType componentType,
        Endpoint endpoint) =>
        new(componentType, endpoint.RawName);

    private async ValueTask<byte[]?> SerializeAsync<TValue>(
        object? message,
        MessageComponentType componentType,
        ProducerEndpoint endpoint)
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
                GetConfluentSerializationContext(componentType, endpoint))
            .ConfigureAwait(false);
    }

    private async Task<TValue?> DeserializeAsync<TValue>(
        byte[]? message,
        MessageComponentType componentType,
        ConsumerEndpoint endpoint)
        where TValue : class
    {
        if (message == null)
            return null;

        AvroDeserializer<TValue> avroDeserializer = new(
            SchemaRegistryClientFactory.GetClient(SchemaRegistryConfig),
            AvroSerializerConfig);

        SerializationContext confluentSerializationContext = GetConfluentSerializationContext(componentType, endpoint);

        return await avroDeserializer.DeserializeAsync(
                new ReadOnlyMemory<byte>(message),
                false,
                confluentSerializationContext)
            .ConfigureAwait(false);
    }
}
