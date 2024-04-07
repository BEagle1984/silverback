// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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
///     Connects to the specified schema registry and serializes the messages in Apache Avro format.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be serialized.
/// </typeparam>
public class AvroMessageSerializer<TMessage> : IAvroMessageSerializer
    where TMessage : class
{
    /// <inheritdoc cref="IAvroMessageSerializer.SchemaRegistryConfig" />
    public SchemaRegistryConfig SchemaRegistryConfig { get; set; } = new();

    /// <inheritdoc cref="IAvroMessageSerializer.AvroSerializerConfig" />
    public AvroSerializerConfig AvroSerializerConfig { get; set; } = new();

    /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
    public async ValueTask<Stream?> SerializeAsync(object? message, MessageHeaderCollection headers, ProducerEndpoint endpoint)
    {
        byte[]? buffer = await SerializeAsync<TMessage>(message, MessageComponentType.Value, endpoint)
            .ConfigureAwait(false);

        return buffer == null ? null : new MemoryStream(buffer);
    }

    /// <inheritdoc cref="IKafkaMessageSerializer.SerializeKey" />
    public byte[] SerializeKey(string key, IReadOnlyCollection<MessageHeader>? headers, KafkaProducerEndpoint endpoint)
    {
        Check.NotNullOrEmpty(key, nameof(key));

        byte[]? serializedKey = SerializeAsync<string>(key, MessageComponentType.Key, endpoint).SafeWait();

        return serializedKey ?? [];
    }

    private static SerializationContext GetConfluentSerializationContext(MessageComponentType componentType, Endpoint endpoint) =>
        new(componentType, endpoint.RawName);

    private async ValueTask<byte[]?> SerializeAsync<TValue>(object? message, MessageComponentType componentType, ProducerEndpoint endpoint)
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
}
