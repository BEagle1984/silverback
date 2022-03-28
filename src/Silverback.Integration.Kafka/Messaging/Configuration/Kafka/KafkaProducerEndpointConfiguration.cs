// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     The Kafka producer endpoint configuration.
/// </summary>
public sealed record KafkaProducerEndpointConfiguration : ProducerEndpointConfiguration<KafkaProducerEndpoint>;
