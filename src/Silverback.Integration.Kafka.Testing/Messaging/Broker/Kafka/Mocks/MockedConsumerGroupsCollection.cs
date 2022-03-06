// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks
{
    [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
    internal class MockedConsumerGroupsCollection : IMockedConsumerGroupsCollection
    {
        private readonly IInMemoryTopicCollection _topicCollection;

        private readonly ConcurrentDictionary<string, MockedConsumerGroup> _groups = new();

        public MockedConsumerGroupsCollection(IInMemoryTopicCollection topicCollection)
        {
            _topicCollection = topicCollection;
        }

        public int Count => _groups.Count;

        public IMockedConsumerGroup Get(ConsumerConfig consumerConfig) =>
            Get(consumerConfig.GroupId, consumerConfig.BootstrapServers);

        public IMockedConsumerGroup Get(string name, string bootstrapServers) =>
            _groups.GetOrAdd(
                $"{name}|{bootstrapServers}",
                _ => new MockedConsumerGroup(name, bootstrapServers, _topicCollection));

        public IEnumerator<IMockedConsumerGroup> GetEnumerator() => _groups.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
