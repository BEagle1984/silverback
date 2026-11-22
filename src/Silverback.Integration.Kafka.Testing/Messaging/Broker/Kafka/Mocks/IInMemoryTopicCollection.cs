// Copyright (c) 2026 Sergio Aquilini
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
    ///     The name of the topic. Must not be empty.
    /// </param>
    /// <param name="clientConfig">
    ///     The client configuration. Its bootstrap servers must be set and must not be empty.
    /// </param>
    /// <returns>
    ///     The in-memory topic.
    /// </returns>
    IInMemoryTopic Get(string name, ClientConfig clientConfig);

    /// <summary>
    ///     Gets the topic with the specified name or creates it on the fly.
    /// </summary>
    /// <param name="name">
    ///     The name of the topic. Must not be empty.
    /// </param>
    /// <param name="bootstrapServers">
    ///     The bootstrap servers string used to identify the target broker. Must not be empty.
    /// </param>
    /// <returns>
    ///     The in-memory topic.
    /// </returns>
    IInMemoryTopic Get(string name, string bootstrapServers);

    /// <summary>
    ///     Creates the topic with the specified name, unless it already exists.
    /// </summary>
    /// <param name="name">
    ///     The name of the topic. Must not be empty.
    /// </param>
    /// <param name="bootstrapServers">
    ///     The bootstrap servers string used to identify the target broker. Must not be empty.
    /// </param>
    /// <param name="partitions">
    ///     The number of partitions, or <c>null</c> to use the number configured in the mock options. If specified, it must be greater
    ///     than or equal to 1.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the topic has been created, <c>false</c> if it already exists.
    /// </returns>
    bool TryCreate(string name, string bootstrapServers, int? partitions = null);
}
