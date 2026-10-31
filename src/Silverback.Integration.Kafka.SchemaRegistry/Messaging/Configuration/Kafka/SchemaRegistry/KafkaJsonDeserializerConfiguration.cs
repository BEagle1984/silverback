// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry.Serdes;

namespace Silverback.Messaging.Configuration.Kafka.SchemaRegistry;

/// <summary>
///     Wraps the <see cref="Confluent.SchemaRegistry.Serdes.JsonDeserializerConfig" />.
/// </summary>
public sealed partial record KafkaJsonDeserializerConfiguration
{
    internal JsonDeserializerConfig ToConfluentConfig() => MapCore();
}
