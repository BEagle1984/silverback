// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;

namespace Silverback.Messaging.Broker.Topics
{
    public class InMemoryTopicCollection : IInMemoryTopicCollection
    {
        private readonly ConcurrentDictionary<string, InMemoryTopic> _topics = new ConcurrentDictionary<string, InMemoryTopic>();

        public IInMemoryTopic GetTopic(string name) => _topics.GetOrAdd(name, _ => new InMemoryTopic(name));
    }
}
