// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     The base class for <see cref="AvroMessageSerializer{TMessage}" />.
/// </summary>
public interface IAvroMessageSerializer : IKafkaMessageSerializer
{
    /// <summary>
    ///     Gets or sets the schema registry configuration.
    /// </summary>
    public SchemaRegistryConfig SchemaRegistryConfig { get; set; }

    /// <summary>
    ///     Gets or sets the Avro serializer configuration.
    /// </summary>
    public AvroSerializerConfig AvroSerializerConfig { get; set; }
}
