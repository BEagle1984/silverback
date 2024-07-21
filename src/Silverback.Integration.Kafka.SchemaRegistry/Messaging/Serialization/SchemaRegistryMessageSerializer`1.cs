// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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
///     The type of the messages to be serialized.
/// </typeparam>
public abstract class SchemaRegistryMessageSerializer<TMessage> : IKafkaMessageSerializer
    where TMessage : class
{
    private readonly IAsyncSerializer<TMessage> _confluentSerializer;

    private readonly IAsyncSerializer<string> _keyConfluentSerializer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SchemaRegistryMessageSerializer{TMessage}" /> class.
    /// </summary>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <param name="confluentSerializer">
    ///     The Confluent serializer to be used to serialize the message.
    /// </param>
    /// <param name="keyConfluentSerializer">
    ///     The Confluent serializer to be used to serialize the key.
    /// </param>
    protected SchemaRegistryMessageSerializer(
        ISchemaRegistryClient schemaRegistryClient,
        IAsyncSerializer<TMessage> confluentSerializer,
        IAsyncSerializer<string> keyConfluentSerializer)
    {
        SchemaRegistryClient = schemaRegistryClient;
        _confluentSerializer = confluentSerializer;
        _keyConfluentSerializer = keyConfluentSerializer;
    }

    /// <summary>
    ///     Gets the schema registry client.
    /// </summary>
    public ISchemaRegistryClient SchemaRegistryClient { get; }

    /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
    public async ValueTask<Stream?> SerializeAsync(object? message, MessageHeaderCollection headers, ProducerEndpoint endpoint)
    {
        Check.NotNull(endpoint, nameof(endpoint));

        if (message == null)
            return null;

        if (message is Stream inputStream)
            return inputStream;

        if (message is byte[] inputBytes)
            return new MemoryStream(inputBytes);

        byte[] buffer = await _confluentSerializer.SerializeAsync(
                (TMessage)message,
                new SerializationContext(MessageComponentType.Value, endpoint.RawName))
            .ConfigureAwait(false);

        return new MemoryStream(buffer);
    }

    /// <inheritdoc cref="IKafkaMessageSerializer.SerializeKey" />
    public byte[] SerializeKey(string key, IReadOnlyCollection<MessageHeader>? headers, KafkaProducerEndpoint endpoint)
    {
        Check.NotNullOrEmpty(key, nameof(key));
        Check.NotNull(endpoint, nameof(endpoint));

        return _keyConfluentSerializer.SerializeAsync(
                key,
                new SerializationContext(MessageComponentType.Key, endpoint.RawName))
            .SafeWait();
    }
}
