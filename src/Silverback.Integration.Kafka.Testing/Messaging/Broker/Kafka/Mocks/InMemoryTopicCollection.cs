// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Confluent.Kafka;
using JetBrains.Annotations;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

internal sealed class InMemoryTopicCollection : IInMemoryTopicCollection
{
    private readonly IMockedKafkaOptions _options;

    private readonly ConcurrentDictionary<string, InMemoryTopic> _topics = new();

    public InMemoryTopicCollection(IMockedKafkaOptions options)
    {
        _options = options;
    }

    public int Count => _topics.Count;

    public IInMemoryTopic Get(string name, ClientConfig clientConfig) =>
        Get(name, clientConfig.BootstrapServers);

    public IInMemoryTopic Get(string name, string bootstrapServers) =>
        _topics.GetOrAdd(
            $"{bootstrapServers.ToUpperInvariant()}|{name}",
            static (_, args) => new InMemoryTopic(args.Name, args.BootstrapServers, args.DefaultPartitionsCount),
            (Name: name, BootstrapServers: bootstrapServers, _options.DefaultPartitionsCount));

    [MustDisposeResource]
    public IEnumerator<IInMemoryTopic> GetEnumerator() => _topics.Values.GetEnumerator();

    [MustDisposeResource]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
