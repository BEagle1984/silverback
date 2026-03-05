// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Connects to the specified schema registry and deserializes the message key from Apache Avro format.
/// </summary>
public class AvroKeyDeserializer : SchemaRegistryKeyDeserializer
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AvroKeyDeserializer" /> class.
    /// </summary>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <param name="avroDeserializerConfig">
    ///     The <see cref="AvroDeserializer{T}" /> configuration.
    /// </param>
    public AvroKeyDeserializer(
        ISchemaRegistryClient schemaRegistryClient,
        AvroDeserializerConfig? avroDeserializerConfig = null)
        : base(
            schemaRegistryClient,
            new AvroDeserializer<string>(schemaRegistryClient, avroDeserializerConfig))
    {
    }
}
