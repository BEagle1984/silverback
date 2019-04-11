// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)
namespace Silverback.Integration.Kafka.ConfigClassGenerator.Messaging
{
    public class KafkaConsumerConfigGen
    {
        private const bool KafkaDefaultAutoCommitEnabled = true;

        /// <summary>
        /// Defines the number of message processed before committing the offset to the server.
        /// The most reliable level is 1 but it reduces throughput.
        /// </summary>
        public int CommitOffsetEach { get; set; } = -1;
    }
}