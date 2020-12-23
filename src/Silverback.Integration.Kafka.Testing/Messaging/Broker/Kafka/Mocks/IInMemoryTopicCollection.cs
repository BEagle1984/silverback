// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Broker.Kafka.Mocks
{
    /// <summary>
    ///     The collection of <see cref="InMemoryTopic" /> being used in the current session.
    /// </summary>
    public interface IInMemoryTopicCollection : IReadOnlyCollection<IInMemoryTopic>
    {
        /// <summary>
        ///     Gets the topic with the specified name or creates it on the fly.
        /// </summary>
        /// <param name="name">
        ///     The name of the topic.
        /// </param>
        IInMemoryTopic this[string name] { get; }
    }
}
