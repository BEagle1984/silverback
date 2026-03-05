// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Connects to the specified schema registry and serializes the message key in Apache Avro format.
/// </summary>
public class AvroKeySerializer : SchemaRegistryKeySerializer
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AvroKeySerializer" /> class.
    /// </summary>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <param name="avroSerializerConfig">
    ///     The <see cref="AvroSerializer{T}" /> configuration.
    /// </param>
    public AvroKeySerializer(
        ISchemaRegistryClient schemaRegistryClient,
        AvroSerializerConfig? avroSerializerConfig = null)
        : base(
            schemaRegistryClient,
            new AvroSerializer<string>(schemaRegistryClient, avroSerializerConfig))
    {
    }
}
