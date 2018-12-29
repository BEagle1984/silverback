// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Broker;

namespace Silverback.Integration.Kafka.Tests.Messaging
{
    [TestFixture]
    public class KafkaClientConfigComparerTests
    {
        private ConcurrentDictionary<Confluent.Kafka.ConsumerConfig, InnerConsumerWrapper> _dictionary;

        [SetUp]
        public void Setup()
        {
            _dictionary = new ConcurrentDictionary<Confluent.Kafka.ConsumerConfig, InnerConsumerWrapper>(new KafkaClientConfigComparer());
        }

        [Test]
        public void Compare_SameInstance_ReturnsTrue()
        {
            var config1 = new Confluent.Kafka.ConsumerConfig
            {
                BootstrapServers = "myserver",
                PartitionAssignmentStrategy = Confluent.Kafka.PartitionAssignmentStrategyType.Range,
                EnableAutoCommit = false
            };
            var config2 = config1;

            _dictionary.TryAdd(config1, null);

            Assert.IsTrue(_dictionary.ContainsKey(config2));
        }

        [Test]
        public void Compare_SameParameters_ReturnsTrue()
        {
            var config1 = new Confluent.Kafka.ConsumerConfig
            {
                BootstrapServers = "myserver",
                PartitionAssignmentStrategy = Confluent.Kafka.PartitionAssignmentStrategyType.Range,
                EnableAutoCommit = false
            };
            var config2 = new Confluent.Kafka.ConsumerConfig
            {
                PartitionAssignmentStrategy = Confluent.Kafka.PartitionAssignmentStrategyType.Range,
                BootstrapServers = "myserver",
                EnableAutoCommit = false
            };

            _dictionary.TryAdd(config1, null);

            Assert.IsTrue(_dictionary.ContainsKey(config2));
        }

        [Test]
        public void Compare_SameParametersDifferentValues_ReturnsFalse()
        {
            var config1 = new Confluent.Kafka.ConsumerConfig
            {
                BootstrapServers = "myserver",
                PartitionAssignmentStrategy = Confluent.Kafka.PartitionAssignmentStrategyType.Range,
                EnableAutoCommit = false
            };
            var config2 = new Confluent.Kafka.ConsumerConfig
            {
                PartitionAssignmentStrategy = Confluent.Kafka.PartitionAssignmentStrategyType.Range,
                BootstrapServers = "myserver",
                EnableAutoCommit = true
            };

            _dictionary.TryAdd(config1, null);

            Assert.IsFalse(_dictionary.ContainsKey(config2));
        }

        [Test]
        public void Compare_DifferentParameters_ReturnsFalse()
        {
            var config1 = new Confluent.Kafka.ConsumerConfig
            {
                BootstrapServers = "myserver",
                PartitionAssignmentStrategy = Confluent.Kafka.PartitionAssignmentStrategyType.Range,
                EnableAutoCommit = false
            };
            var config2 = new Confluent.Kafka.ConsumerConfig
            {
                PartitionAssignmentStrategy = Confluent.Kafka.PartitionAssignmentStrategyType.Range,
                BootstrapServers = "myserver"
            };

            _dictionary.TryAdd(config1, null);

            Assert.IsFalse(_dictionary.ContainsKey(config2));
        }
    }
}
