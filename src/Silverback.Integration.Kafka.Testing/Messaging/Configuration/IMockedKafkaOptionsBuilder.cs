// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Exposes the methods to configure the mocked Kafka.
    /// </summary>
    public interface IMockedKafkaOptionsBuilder
    {
        /// <summary>
        ///     Specifies the default number of partitions to be created per each topic. The default is 5.
        /// </summary>
        /// <param name="partitionsCount">
        ///     The number of partitions.
        /// </param>
        /// <returns>
        ///     The <see cref="IMockedKafkaOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMockedKafkaOptionsBuilder WithDefaultPartitionsCount(int partitionsCount);
    }
}
