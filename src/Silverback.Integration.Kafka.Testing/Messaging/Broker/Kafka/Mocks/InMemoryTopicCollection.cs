// Copyright (c) 2026 Sergio Aquilini
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
            GetKey(name, bootstrapServers),
            static (_, args) => new InMemoryTopic(args.Name, args.BootstrapServers, GetPartitionsCount(args.Name, args.Options)),
            (Name: name, BootstrapServers: bootstrapServers, Options: _options));

    public bool TryCreate(string name, string bootstrapServers, int? partitions = null) =>
        _topics.TryAdd(
            GetKey(name, bootstrapServers),
            new InMemoryTopic(name, bootstrapServers, partitions ?? GetPartitionsCount(name, _options)));

    [MustDisposeResource]
    public IEnumerator<IInMemoryTopic> GetEnumerator() => _topics.Values.GetEnumerator();

    [MustDisposeResource]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static string GetKey(string name, string bootstrapServers) => $"{bootstrapServers.ToUpperInvariant()}|{name}";

    private static int GetPartitionsCount(string name, IMockedKafkaOptions options) =>
        options.TopicPartitionsCount.TryGetValue(name, out int partitionsCount) ? partitionsCount : options.DefaultPartitionsCount;
}
