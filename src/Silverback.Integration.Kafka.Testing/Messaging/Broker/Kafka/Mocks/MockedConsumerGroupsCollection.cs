// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Confluent.Kafka;
using JetBrains.Annotations;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

internal class MockedConsumerGroupsCollection : IMockedConsumerGroupsCollection
{
    private readonly IInMemoryTopicCollection _topicCollection;

    private readonly ConcurrentDictionary<string, MockedConsumerGroup> _groups = new();

    public MockedConsumerGroupsCollection(IInMemoryTopicCollection topicCollection)
    {
        _topicCollection = topicCollection;

        // Both are singletons, so the handler doesn't need to be removed
        if (topicCollection is InMemoryTopicCollection inMemoryTopicCollection)
            inMemoryTopicCollection.TopicCreated += OnTopicCreated;
    }

    public int Count => _groups.Count;

    public IMockedConsumerGroup Get(ConsumerConfig consumerConfig) =>
        Get(consumerConfig.GroupId, consumerConfig.BootstrapServers);

    public IMockedConsumerGroup Get(string name, string bootstrapServers) =>
        _groups.GetOrAdd(
            $"{name}|{bootstrapServers}",
            static (_, args) => new MockedConsumerGroup(args.name, args.bootstrapServers, args._topicCollection),
            (name, bootstrapServers, _topicCollection));

    [MustDisposeResource]
    public IEnumerator<IMockedConsumerGroup> GetEnumerator() => _groups.Values.GetEnumerator();

    [MustDisposeResource]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void OnTopicCreated(object? sender, IInMemoryTopic topic)
    {
        foreach (MockedConsumerGroup group in _groups.Values)
        {
            group.OnTopicCreated(topic);
        }
    }
}
