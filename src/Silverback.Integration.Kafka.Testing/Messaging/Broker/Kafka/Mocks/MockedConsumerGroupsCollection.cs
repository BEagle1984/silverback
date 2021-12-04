// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

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
            static (_, parameters) => new MockedConsumerGroup(parameters.Name, parameters.BootstrapServers, parameters.TopicCollection),
            new NewConsumerGroupParameters(name, bootstrapServers, _topicCollection));

    public IEnumerator<IMockedConsumerGroup> GetEnumerator() => _groups.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "False positive, remove suppression once record struct is handled properly")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "False positive, remove suppression once record struct is handled properly")]
    private record struct NewConsumerGroupParameters(string Name, string BootstrapServers, IInMemoryTopicCollection TopicCollection);
}
