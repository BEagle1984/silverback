// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Google.Protobuf;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Connects to the specified schema registry and deserializes the messages from Protobuf format.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be deserialized.
/// </typeparam>
public class ProtobufMessageDeserializer<TMessage> : SchemaRegistryMessageDeserializer<TMessage>
    where TMessage : class, IMessage<TMessage>, new()
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProtobufMessageDeserializer{TMessage}" /> class.
    /// </summary>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <param name="protobufDeserializerConfig">
    ///     The <see cref="ProtobufDeserializer{T}" /> configuration.
    /// </param>
    public ProtobufMessageDeserializer(
        ISchemaRegistryClient schemaRegistryClient,
        ProtobufDeserializerConfig? protobufDeserializerConfig = null)
        : base(
            schemaRegistryClient,
            new ProtobufDeserializer<TMessage>(schemaRegistryClient, protobufDeserializerConfig))
    {
        ProtobufDeserializerConfig = protobufDeserializerConfig;
    }

    /// <summary>
    ///     Gets the <see cref="ProtobufDeserializer{T}" /> configuration.
    /// </summary>
    public ProtobufDeserializerConfig? ProtobufDeserializerConfig { get; }

    /// <inheritdoc cref="SchemaRegistryMessageDeserializer{TMessage}.GetCompatibleSerializer" />
    public override IMessageSerializer GetCompatibleSerializer() =>
        new ProtobufMessageSerializer<TMessage>(
            SchemaRegistryClient,
            new ProtobufSerializerConfig(ProtobufDeserializerConfig));
}
