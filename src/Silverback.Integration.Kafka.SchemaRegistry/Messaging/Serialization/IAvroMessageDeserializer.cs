// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Connects to the specified schema registry and deserializes the messages from Apache Avro format.
/// </summary>
public interface IAvroMessageDeserializer : IKafkaMessageDeserializer
{
    /// <summary>
    ///     Gets or sets the schema registry configuration.
    /// </summary>
    public SchemaRegistryConfig SchemaRegistryConfig { get; set; }

    /// <summary>
    ///     Gets or sets the Avro serializer configuration.
    /// </summary>
    public AvroDeserializerConfig AvroDeserializerConfig { get; set; }
}
