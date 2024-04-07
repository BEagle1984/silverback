// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

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
    /// <param name="clientConfig">
    ///     The client configuration.
    /// </param>
    /// <returns>
    ///     The in-memory topic.
    /// </returns>
    IInMemoryTopic Get(string name, ClientConfig clientConfig);

    /// <summary>
    ///     Gets the topic with the specified name or creates it on the fly.
    /// </summary>
    /// <param name="name">
    ///     The name of the topic.
    /// </param>
    /// <param name="bootstrapServers">
    ///     The bootstrap servers string used to identify the target broker.
    /// </param>
    /// <returns>
    ///     The in-memory topic.
    /// </returns>
    IInMemoryTopic Get(string name, string bootstrapServers);
}
