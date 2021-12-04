// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

[SuppressMessage("", "CA1812", Justification = "Class used via DI")]
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
            static (_, parameters) => new InMemoryTopic(parameters.Name, parameters.BootstrapServers, parameters.PartitionsCount),
            new NewTopicParameters(name, bootstrapServers, _options.DefaultPartitionsCount));

    public IEnumerator<IInMemoryTopic> GetEnumerator() => _topics.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "False positive, remove suppression once record struct is handled properly")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "False positive, remove suppression once record struct is handled properly")]
    private record struct NewTopicParameters(string Name, string BootstrapServers, int PartitionsCount);
}
