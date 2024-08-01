// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Google.Protobuf;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Connects to the specified schema registry and serializes the messages in Protobuf format.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be serialized.
/// </typeparam>
public class ProtobufMessageSerializer<TMessage> : SchemaRegistryMessageSerializer<TMessage>
    where TMessage : class, IMessage<TMessage>, new()
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProtobufMessageSerializer{TMessage}" /> class.
    /// </summary>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <param name="protobufSerializerConfig">
    ///     The <see cref="ProtobufSerializer{T}" /> configuration.
    /// </param>
    public ProtobufMessageSerializer(
        ISchemaRegistryClient schemaRegistryClient,
        ProtobufSerializerConfig? protobufSerializerConfig = null)
        : base(
            schemaRegistryClient,
            new ProtobufSerializer<TMessage>(schemaRegistryClient, protobufSerializerConfig))
    {
        ProtobufSerializerConfig = protobufSerializerConfig;
    }

    /// <summary>
    ///     Gets the <see cref="ProtobufSerializer{T}" /> configuration.
    /// </summary>
    public ProtobufSerializerConfig? ProtobufSerializerConfig { get; }
}
