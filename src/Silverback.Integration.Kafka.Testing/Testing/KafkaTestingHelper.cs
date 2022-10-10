// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Kafka.Mocks;

namespace Silverback.Testing;

/// <inheritdoc cref="IKafkaTestingHelper" />
public partial class KafkaTestingHelper : TestingHelper, IKafkaTestingHelper
{
    private readonly IInMemoryTopicCollection? _topics;

    private readonly IMockedConsumerGroupsCollection? _groups;

    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaTestingHelper" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger" />.
    /// </param>
    public KafkaTestingHelper(IServiceProvider serviceProvider, ILogger<KafkaTestingHelper> logger)
        : base(serviceProvider, logger)
    {
        _topics = serviceProvider.GetService<IInMemoryTopicCollection>();
        _groups = serviceProvider.GetService<IMockedConsumerGroupsCollection>();
        _serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
    }

    /// <inheritdoc cref="IKafkaTestingHelper.ConsumerGroups" />
    public IReadOnlyCollection<IMockedConsumerGroup> ConsumerGroups =>
        (IReadOnlyCollection<IMockedConsumerGroup>?)_groups ?? Array.Empty<IMockedConsumerGroup>();

    /// <inheritdoc cref="IKafkaTestingHelper.GetConsumerGroup(string)" />
    public IMockedConsumerGroup GetConsumerGroup(string groupId)
    {
        if (_groups == null)
            throw new InvalidOperationException("The IInMemoryTopicCollection is not initialized.");

        return _groups.First(group => group.GroupId == groupId);
    }

    /// <inheritdoc cref="IKafkaTestingHelper.GetConsumerGroup(string,string)" />
    public IMockedConsumerGroup GetConsumerGroup(string groupId, string bootstrapServers)
    {
        if (_groups == null)
            throw new InvalidOperationException("The IInMemoryTopicCollection is not initialized.");

        return _groups.First(group => group.GroupId == groupId && group.BootstrapServers == bootstrapServers);
    }

    /// <inheritdoc cref="IKafkaTestingHelper.GetTopic(string,string?)" />
    public IInMemoryTopic GetTopic(string name, string? bootstrapServers = null)
    {
        if (_topics == null)
            throw new InvalidOperationException("The IInMemoryTopicCollection is not initialized.");

        List<IInMemoryTopic> topics = _topics.Where(
                topic =>
                    topic.Name == name &&
                    (bootstrapServers == null || string.Equals(
                        bootstrapServers,
                        topic.BootstrapServers,
                        StringComparison.OrdinalIgnoreCase)))
            .ToList();

        switch (topics.Count)
        {
            case > 1:
                throw new InvalidOperationException($"More than one topic '{name}' found. Try specifying the bootstrap servers.");
            case 0 when bootstrapServers == null:
                string[] distinctBootstrapServers = _topics.Select(topic => topic.BootstrapServers).Distinct().ToArray();

                if (distinctBootstrapServers.Length == 1)
                    return GetTopic(name, distinctBootstrapServers[0]);

                throw new InvalidOperationException($"Topic '{name}' not found and cannot be created on-the-fly. Try specifying the bootstrap servers.");
            case 0:
                return _topics.Get(name, bootstrapServers);
            default:
                return topics.Single();
        }
    }

    /// <inheritdoc cref="TestingHelper.WaitUntilAllMessagesAreConsumedCoreAsync(CancellationToken)" />
    protected override Task WaitUntilAllMessagesAreConsumedCoreAsync(CancellationToken cancellationToken) =>
        _groups == null
            ? Task.CompletedTask
            : Task.WhenAll(_groups.Select(group => group.WaitUntilAllMessagesAreConsumedAsync(cancellationToken)));
}
