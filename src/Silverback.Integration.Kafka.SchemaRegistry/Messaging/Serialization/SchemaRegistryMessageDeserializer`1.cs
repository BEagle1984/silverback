// Copyright (c) 2025 Sergio Aquilini
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
///     Connects to the specified schema registry and deserializes the messages.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be deserialized.
/// </typeparam>
public abstract class SchemaRegistryMessageDeserializer<TMessage> : IMessageDeserializer
    where TMessage : class
{
    private readonly IAsyncDeserializer<TMessage> _confluentDeserializer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SchemaRegistryMessageDeserializer{TMessage}" /> class.
    /// </summary>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <param name="confluentDeserializer">
    ///     The Confluent deserializer to be used to deserialize the message.
    /// </param>
    protected SchemaRegistryMessageDeserializer(
        ISchemaRegistryClient schemaRegistryClient,
        IAsyncDeserializer<TMessage> confluentDeserializer)
    {
        SchemaRegistryClient = schemaRegistryClient;
        _confluentDeserializer = confluentDeserializer;
    }

    /// <inheritdoc cref="IMessageDeserializer.RequireHeaders" />
    public bool RequireHeaders => false;

    /// <summary>
    ///     Gets the schema registry client.
    /// </summary>
    public ISchemaRegistryClient SchemaRegistryClient { get; }

    /// <inheritdoc cref="IMessageDeserializer.DeserializeAsync" />
    public async ValueTask<DeserializedMessage> DeserializeAsync(
        Stream? messageStream,
        MessageHeaderCollection headers,
        ConsumerEndpoint endpoint)
    {
        Check.NotNull(endpoint, nameof(endpoint));

        byte[]? buffer = await messageStream.ReadAllAsync().ConfigureAwait(false);

        if (buffer == null)
            return new DeserializedMessage(null, typeof(TMessage));

        TMessage deserialized = await _confluentDeserializer.DeserializeAsync(
                new ReadOnlyMemory<byte>(buffer),
                false,
                new SerializationContext(MessageComponentType.Value, endpoint.RawName))
            .ConfigureAwait(false);

        return new DeserializedMessage(deserialized, deserialized.GetType());
    }

    /// <inheritdoc cref="IMessageDeserializer.GetCompatibleSerializer" />
    public abstract IMessageSerializer GetCompatibleSerializer();
}
