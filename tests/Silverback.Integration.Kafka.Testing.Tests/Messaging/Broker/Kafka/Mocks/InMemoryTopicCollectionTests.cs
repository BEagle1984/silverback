// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Shouldly;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Broker.Kafka.Mocks;

public class InMemoryTopicCollectionTests
{
    private const string BootstrapServers = "PLAINTEXT://mock";

    [Fact]
    public void Get_ShouldRaiseTopicCreatedOnce_WhenTopicIsCreated()
    {
        InMemoryTopicCollection topics = new(new MockedKafkaOptions());
        List<IInMemoryTopic> createdTopics = [];
        topics.TopicCreated += (_, topic) => createdTopics.Add(topic);

        IInMemoryTopic topic1 = topics.Get("topic1", BootstrapServers);
        IInMemoryTopic topic1Again = topics.Get("topic1", BootstrapServers.ToUpperInvariant());
        IInMemoryTopic topic2 = topics.Get("topic2", BootstrapServers);

        topic1Again.ShouldBeSameAs(topic1);
        createdTopics.ShouldBe([topic1, topic2]);
    }

    [Fact]
    public void TryCreate_ShouldRaiseTopicCreated_WhenTopicIsCreated()
    {
        InMemoryTopicCollection topics = new(new MockedKafkaOptions());
        List<IInMemoryTopic> createdTopics = [];
        topics.TopicCreated += (_, topic) => createdTopics.Add(topic);

        topics.TryCreate("topic1", BootstrapServers, 3).ShouldBeTrue();
        topics.TryCreate("topic1", BootstrapServers, 3).ShouldBeFalse();

        createdTopics.Count.ShouldBe(1);
        createdTopics[0].ShouldBeSameAs(topics.Get("topic1", BootstrapServers));
        createdTopics[0].Partitions.Count.ShouldBe(3);
    }
}
