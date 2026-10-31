// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry.Serdes;

namespace Silverback.Messaging.Configuration.Kafka.SchemaRegistry;

/// <summary>
///     Wraps the <see cref="Confluent.SchemaRegistry.Serdes.ProtobufDeserializerConfig" />.
/// </summary>
public sealed partial record KafkaProtobufDeserializerConfiguration
{
    internal ProtobufDeserializerConfig ToConfluentConfig() => MapCore();
}
