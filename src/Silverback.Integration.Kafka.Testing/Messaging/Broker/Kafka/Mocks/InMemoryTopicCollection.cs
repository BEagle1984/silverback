// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks
{
    internal class InMemoryTopicCollection : IInMemoryTopicCollection
    {
        private readonly IMockedKafkaOptions _options;

        private readonly ConcurrentDictionary<string, InMemoryTopic> _topics = new();

        private readonly object _consumersLock = new();

        public InMemoryTopicCollection(IMockedKafkaOptions options)
        {
            _options = options;
        }

        public int Count => _topics.Count;

        public IInMemoryTopic this[string name] => _topics.GetOrAdd(
            name,
            _ => new InMemoryTopic(name, _options.DefaultPartitionsCount, _consumersLock));

        public IEnumerator<IInMemoryTopic> GetEnumerator() => _topics.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
