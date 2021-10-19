// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     The base class for <see cref="AvroMessageSerializer{TMessage}" />.
/// </summary>
public abstract class AvroMessageSerializerBase : IKafkaMessageSerializer
{
    /// <inheritdoc cref="IMessageSerializer.RequireHeaders" />
    public bool RequireHeaders => false;

    /// <summary>
    ///     Gets or sets the schema registry configuration.
    /// </summary>
    public SchemaRegistryConfig SchemaRegistryConfig { get; set; } = new();

    /// <summary>
    ///     Gets or sets the Avro serializer configuration.
    /// </summary>
    public AvroSerializerConfig AvroSerializerConfig { get; set; } = new();

    /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
    public abstract ValueTask<Stream?> SerializeAsync(object? message, MessageHeaderCollection headers, ProducerEndpoint endpoint);

    /// <inheritdoc cref="IMessageSerializer.DeserializeAsync" />
    public abstract ValueTask<DeserializedMessage> DeserializeAsync(
        Stream? messageStream,
        MessageHeaderCollection headers,
        ConsumerEndpoint endpoint);

    /// <inheritdoc cref="IKafkaMessageSerializer.SerializeKey" />
    public abstract byte[] SerializeKey(string key, IReadOnlyCollection<MessageHeader> headers, KafkaProducerEndpoint endpoint);

    /// <inheritdoc cref="IKafkaMessageSerializer.DeserializeKey" />
    public abstract string DeserializeKey(byte[] key, IReadOnlyCollection<MessageHeader> headers, KafkaConsumerEndpoint endpoint);
}
