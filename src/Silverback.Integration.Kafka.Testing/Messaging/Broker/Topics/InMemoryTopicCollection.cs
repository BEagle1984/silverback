// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Broker.Topics
{
    /// <inheritdoc cref="IInMemoryTopicCollection" />
    public class InMemoryTopicCollection : IInMemoryTopicCollection
    {
        private readonly IMockedKafkaOptions _options;

        private readonly ConcurrentDictionary<string, InMemoryTopic> _topics =
            new ConcurrentDictionary<string, InMemoryTopic>();

        private readonly object _consumersLock = new object();

        /// <summary>
        ///     Initializes a new instance of the <see cref="InMemoryTopicCollection" /> class.
        /// </summary>
        /// <param name="options">
        ///     The mocked Kafka configuration.
        /// </param>
        public InMemoryTopicCollection(IMockedKafkaOptions options)
        {
            _options = options;
        }

        /// <inheritdoc cref="IReadOnlyCollection{T}.Count" />
        public int Count => _topics.Count;

        /// <inheritdoc cref="IInMemoryTopicCollection.this" />
        public IInMemoryTopic this[string name] => _topics.GetOrAdd(
            name,
            _ => new InMemoryTopic(name, _options.DefaultPartitionsCount, _consumersLock));

        /// <inheritdoc cref="IEnumerable{IInMemoryTopic}.GetEnumerator()" />
        public IEnumerator<IInMemoryTopic> GetEnumerator() => _topics.Values.GetEnumerator();

        /// <inheritdoc cref="IEnumerable.GetEnumerator()" />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
