// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using JetBrains.Annotations;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

internal sealed class InMemoryTopicCollection : IInMemoryTopicCollection
{
    private readonly IMockedKafkaOptions _options;

    private readonly ConcurrentDictionary<string, InMemoryTopic> _topics = new();

    // Serializes the creation of the topics and the notification of the consumer groups
    private readonly System.Threading.Lock _creationLock = new();

    public InMemoryTopicCollection(IMockedKafkaOptions options)
    {
        _options = options;
    }

    // Raised synchronously while holding the lock, so that Get returns the topic to the other threads only after the notification (the
    // enumeration can instead return it before)
    public event EventHandler<IInMemoryTopic>? TopicCreated;

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "The lock only serializes the creation")]
    public int Count => _topics.Count;

    public IInMemoryTopic Get(string name, ClientConfig clientConfig) =>
        Get(name, clientConfig.BootstrapServers);

    public IInMemoryTopic Get(string name, string bootstrapServers)
    {
        string key = GetKey(name, bootstrapServers);

        lock (_creationLock)
        {
            if (_topics.TryGetValue(key, out InMemoryTopic? topic))
                return topic;

            topic = new InMemoryTopic(name, bootstrapServers, GetPartitionsCount(name, _options));
            _topics[key] = topic;
            TopicCreated?.Invoke(this, topic);

            return topic;
        }
    }

    public bool TryCreate(string name, string bootstrapServers, int? partitions = null)
    {
        lock (_creationLock)
        {
            InMemoryTopic topic = new(name, bootstrapServers, partitions ?? GetPartitionsCount(name, _options));

            if (!_topics.TryAdd(GetKey(name, bootstrapServers), topic))
                return false;

            TopicCreated?.Invoke(this, topic);

            return true;
        }
    }

    [MustDisposeResource]
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "The lock only serializes the creation")]
    public IEnumerator<IInMemoryTopic> GetEnumerator() => _topics.Values.GetEnumerator();

    [MustDisposeResource]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static string GetKey(string name, string bootstrapServers) => $"{bootstrapServers.ToUpperInvariant()}|{name}";

    private static int GetPartitionsCount(string name, IMockedKafkaOptions options) =>
        options.TopicPartitionsCount.TryGetValue(name, out int partitionsCount) ? partitionsCount : options.DefaultPartitionsCount;
}
