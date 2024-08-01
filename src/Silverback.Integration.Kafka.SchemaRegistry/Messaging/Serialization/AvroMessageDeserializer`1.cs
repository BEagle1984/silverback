// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Connects to the specified schema registry and deserializes the messages from Apache Avro format.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be deserialized.
/// </typeparam>
public class AvroMessageDeserializer<TMessage> : SchemaRegistryMessageDeserializer<TMessage>
    where TMessage : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AvroMessageDeserializer{TMessage}" /> class.
    /// </summary>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <param name="avroDeserializerConfig">
    ///     The <see cref="AvroDeserializer{T}" /> configuration.
    /// </param>
    public AvroMessageDeserializer(
        ISchemaRegistryClient schemaRegistryClient,
        AvroDeserializerConfig? avroDeserializerConfig = null)
        : base(
            schemaRegistryClient,
            new AvroDeserializer<TMessage>(schemaRegistryClient, avroDeserializerConfig))
    {
        AvroDeserializerConfig = avroDeserializerConfig;
    }

    /// <summary>
    ///     Gets the <see cref="AvroDeserializer{T}" /> configuration.
    /// </summary>
    public AvroDeserializerConfig? AvroDeserializerConfig { get; }

    /// <inheritdoc cref="SchemaRegistryMessageDeserializer{TMessage}.GetCompatibleSerializer" />
    public override IMessageSerializer GetCompatibleSerializer() =>
        new AvroMessageSerializer<TMessage>(
            SchemaRegistryClient,
            new AvroSerializerConfig(AvroDeserializerConfig));
}
