﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks
{
    [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
    internal sealed class InMemoryTopicCollection : IEnumerable<IInMemoryTopic>
    {
        private readonly IMockedKafkaOptions _options;

        private readonly ConcurrentDictionary<string, InMemoryTopic> _topics = new();

        private readonly object _consumersLock = new();

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
                _ => new InMemoryTopic(
                    name,
                    bootstrapServers,
                    _options.TopicPartitionsCount.TryGetValue(name, out int partitionsCount) ? partitionsCount : _options.DefaultPartitionsCount,
                    _consumersLock));

        public IEnumerator<IInMemoryTopic> GetEnumerator() => _topics.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
