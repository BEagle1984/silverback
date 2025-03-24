// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Connects to the specified schema registry and serializes the messages in Apache Avro format.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be serialized.
/// </typeparam>
public class AvroMessageSerializer<TMessage> : SchemaRegistryMessageSerializer<TMessage>
    where TMessage : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AvroMessageSerializer{TMessage}" /> class.
    /// </summary>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <param name="avroSerializerConfig">
    ///     The <see cref="AvroSerializer{T}" /> configuration.
    /// </param>
    public AvroMessageSerializer(
        ISchemaRegistryClient schemaRegistryClient,
        AvroSerializerConfig? avroSerializerConfig = null)
        : base(
            schemaRegistryClient,
            new AvroSerializer<TMessage>(schemaRegistryClient, avroSerializerConfig))
    {
        AvroSerializerConfig = avroSerializerConfig;
    }

    /// <summary>
    ///     Gets the <see cref="AvroSerializer{T}" /> configuration.
    /// </summary>
    public AvroSerializerConfig? AvroSerializerConfig { get; }
}
