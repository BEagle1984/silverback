// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Connects to the specified schema registry and serializes the message key.
/// </summary>
public abstract class SchemaRegistryKeySerializer : IMessageKeySerializer
{
    private readonly IAsyncSerializer<string> _confluentSerializer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SchemaRegistryKeySerializer" /> class.
    /// </summary>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <param name="confluentSerializer">
    ///     The Confluent serializer to be used to serialize the key.
    /// </param>
    protected SchemaRegistryKeySerializer(
        ISchemaRegistryClient schemaRegistryClient,
        IAsyncSerializer<string> confluentSerializer)
    {
        SchemaRegistryClient = schemaRegistryClient;
        _confluentSerializer = confluentSerializer;
    }

    /// <summary>
    ///     Gets the schema registry client.
    /// </summary>
    public ISchemaRegistryClient SchemaRegistryClient { get; }

    /// <inheritdoc cref="IMessageKeySerializer.SerializeAsync" />
    public async ValueTask<Stream?> SerializeAsync(
        string? key,
        MessageHeaderCollection headers,
        ProducerEndpoint endpoint)
    {
        Check.NotNull(endpoint, nameof(endpoint));

        if (key == null)
            return null;

        byte[] buffer = await _confluentSerializer.SerializeAsync(
                key,
                new SerializationContext(MessageComponentType.Key, endpoint.RawName))
            .ConfigureAwait(false);

        return new MemoryStream(buffer);
    }
}
