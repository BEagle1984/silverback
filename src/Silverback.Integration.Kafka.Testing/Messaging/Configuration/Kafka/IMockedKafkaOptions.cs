// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration.Kafka
{
    /// <summary>
    ///     Stores the mocked Kafka configuration.
    /// </summary>
    public interface IMockedKafkaOptions
    {
        /// <summary>
        ///     Gets or sets the default number of partitions to be created per each topic. The default is 5.
        /// </summary>
        public int DefaultPartitionsCount { get; set; }
    }
}
