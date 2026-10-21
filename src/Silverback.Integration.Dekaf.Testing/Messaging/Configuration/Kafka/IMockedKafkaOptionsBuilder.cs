// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Configures the in-memory cluster used by the mocked Kafka.
/// </summary>
public interface IMockedKafkaOptionsBuilder
{
    /// <summary>
    ///     Sets the number of partitions of the topics that are created on the fly. The default is 1.
    /// </summary>
    /// <param name="partitionsCount">
    ///     The number of partitions.
    /// </param>
    /// <returns>
    ///     The <see cref="IMockedKafkaOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    IMockedKafkaOptionsBuilder WithDefaultPartitionsCount(int partitionsCount);
}
