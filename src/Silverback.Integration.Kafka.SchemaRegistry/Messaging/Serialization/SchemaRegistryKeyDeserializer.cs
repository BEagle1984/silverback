// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Connects to the specified schema registry and deserializes the message key.
/// </summary>
public abstract class SchemaRegistryKeyDeserializer : IMessageKeyDeserializer
{
    private readonly IAsyncDeserializer<string> _confluentDeserializer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SchemaRegistryKeyDeserializer" /> class.
    /// </summary>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <param name="confluentDeserializer">
    ///     The Confluent deserializer to be used to deserialize the key.
    /// </param>
    protected SchemaRegistryKeyDeserializer(
        ISchemaRegistryClient schemaRegistryClient,
        IAsyncDeserializer<string> confluentDeserializer)
    {
        SchemaRegistryClient = schemaRegistryClient;
        _confluentDeserializer = confluentDeserializer;
    }

    /// <summary>
    ///     Gets the schema registry client.
    /// </summary>
    public ISchemaRegistryClient SchemaRegistryClient { get; }

    /// <inheritdoc cref="IMessageKeyDeserializer.DeserializeAsync" />
    public async ValueTask<string?> DeserializeAsync(
        Stream? keyStream,
        MessageHeaderCollection headers,
        ConsumerEndpoint endpoint)
    {
        Check.NotNull(endpoint, nameof(endpoint));

        byte[]? buffer = await keyStream.ReadAllAsync().ConfigureAwait(false);

        if (buffer == null)
            return null;

        string deserialized = await _confluentDeserializer.DeserializeAsync(
                new ReadOnlyMemory<byte>(buffer),
                false,
                new SerializationContext(MessageComponentType.Key, endpoint.RawName))
            .ConfigureAwait(false);

        return deserialized?.ToString();
    }
}
