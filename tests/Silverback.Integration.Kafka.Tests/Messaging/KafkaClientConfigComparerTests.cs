// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging
{
    public class KafkaClientConfigComparerTests
    {
        private readonly ConcurrentDictionary<Confluent.Kafka.ConsumerConfig, InnerConsumerWrapper> _dictionary;

        public KafkaClientConfigComparerTests()
        {
            _dictionary = new ConcurrentDictionary<Confluent.Kafka.ConsumerConfig, InnerConsumerWrapper>(new KafkaClientConfigComparer());
        }

        [Fact]
        public void Compare_SameInstance_ReturnsTrue()
        {
            var config1 = new Confluent.Kafka.ConsumerConfig
            {
                BootstrapServers = "myserver",
                PartitionAssignmentStrategy = Confluent.Kafka.PartitionAssignmentStrategy.Range,
                EnableAutoCommit = false
            };
            var config2 = config1;

            _dictionary.TryAdd(config1, null);

           _dictionary.Should().ContainKey(config2);
        }

        [Fact]
        public void Compare_SameParameters_ReturnsTrue()
        {
            var config1 = new Confluent.Kafka.ConsumerConfig
            {
                BootstrapServers = "myserver",
                PartitionAssignmentStrategy = Confluent.Kafka.PartitionAssignmentStrategy.Range,
                EnableAutoCommit = false
            };
            var config2 = new Confluent.Kafka.ConsumerConfig
            {
                PartitionAssignmentStrategy = Confluent.Kafka.PartitionAssignmentStrategy.Range,
                BootstrapServers = "myserver",
                EnableAutoCommit = false
            };

            _dictionary.TryAdd(config1, null);

            _dictionary.Should().ContainKey(config2);
        }

        [Fact]
        public void Compare_SameParametersDifferentValues_ReturnsFalse()
        {
            var config1 = new Confluent.Kafka.ConsumerConfig
            {
                BootstrapServers = "myserver",
                PartitionAssignmentStrategy = Confluent.Kafka.PartitionAssignmentStrategy.Range,
                EnableAutoCommit = false
            };
            var config2 = new Confluent.Kafka.ConsumerConfig
            {
                PartitionAssignmentStrategy = Confluent.Kafka.PartitionAssignmentStrategy.Range,
                BootstrapServers = "myserver",
                EnableAutoCommit = true
            };

            _dictionary.TryAdd(config1, null);

            _dictionary.Should().NotContainKey(config2);
        }

        [Fact]
        public void Compare_DifferentParameters_ReturnsFalse()
        {
            var config1 = new Confluent.Kafka.ConsumerConfig
            {
                BootstrapServers = "myserver",
                PartitionAssignmentStrategy = Confluent.Kafka.PartitionAssignmentStrategy.Range,
                EnableAutoCommit = false
            };
            var config2 = new Confluent.Kafka.ConsumerConfig
            {
                PartitionAssignmentStrategy = Confluent.Kafka.PartitionAssignmentStrategy.Range,
                BootstrapServers = "myserver"
            };

            _dictionary.TryAdd(config1, null);

            _dictionary.Should().NotContainKey(config2);
        }
    }
}
