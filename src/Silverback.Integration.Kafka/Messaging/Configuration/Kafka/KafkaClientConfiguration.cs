// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Wraps the <see cref="Confluent.Kafka.ConsumerConfig" />.
/// </summary>
public record KafkaClientConfiguration : KafkaClientConfiguration<ClientConfig>
{
    internal override ClientConfig ToConfluentConfig() => MapCore();
}
