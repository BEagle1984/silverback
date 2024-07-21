// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
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
public abstract class SchemaRegistryMessageDeserializer<TMessage> : IKafkaMessageDeserializer
    where TMessage : class
{
    private readonly IAsyncDeserializer<TMessage> _confluentDeserializer;

    private readonly IAsyncDeserializer<string> _keyConfluentDeserializer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SchemaRegistryMessageDeserializer{TMessage}" /> class.
    /// </summary>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <param name="confluentDeserializer">
    ///     The Confluent deserializer to be used to deserialize the message.
    /// </param>
    /// <param name="keyConfluentDeserializer">
    ///     The Confluent deserializer to be used to deserialize the key.
    /// </param>
    protected SchemaRegistryMessageDeserializer(
        ISchemaRegistryClient schemaRegistryClient,
        IAsyncDeserializer<TMessage> confluentDeserializer,
        IAsyncDeserializer<string> keyConfluentDeserializer)
    {
        SchemaRegistryClient = schemaRegistryClient;
        _confluentDeserializer = confluentDeserializer;
        _keyConfluentDeserializer = keyConfluentDeserializer;
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

    /// <inheritdoc cref="IKafkaMessageDeserializer.DeserializeKey" />
    public string DeserializeKey(byte[] key, IReadOnlyCollection<MessageHeader>? headers, KafkaConsumerEndpoint endpoint)
    {
        Check.NotNull(key, nameof(key));
        Check.NotNull(endpoint, nameof(endpoint));

        return _keyConfluentDeserializer.DeserializeAsync(
            new ReadOnlyMemory<byte>(key),
            false,
            new SerializationContext(MessageComponentType.Value, endpoint.RawName)).SafeWait();
    }
}
