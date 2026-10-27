// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Dekaf.Testing;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <inheritdoc cref="IMockedKafkaOptionsBuilder" />
internal sealed class MockedKafkaOptionsBuilder : IMockedKafkaOptionsBuilder
{
    public InMemoryKafkaClusterOptions ClusterOptions { get; } = new();

    public IMockedKafkaOptionsBuilder WithDefaultPartitionsCount(int partitionsCount)
    {
        ClusterOptions.DefaultPartitionCount = Check.GreaterThan(partitionsCount, nameof(partitionsCount), 0);
        return this;
    }
}
