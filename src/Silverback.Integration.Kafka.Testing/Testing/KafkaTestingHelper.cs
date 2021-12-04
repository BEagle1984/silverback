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
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka.Mocks;

namespace Silverback.Testing;

/// <inheritdoc cref="IKafkaTestingHelper" />
public class KafkaTestingHelper : TestingHelper<KafkaBroker>, IKafkaTestingHelper
{
    private readonly IInMemoryTopicCollection? _topics;

    private readonly IMockedConsumerGroupsCollection? _groups;

    private readonly KafkaBroker _kafkaBroker;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaTestingHelper" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger" />.
    /// </param>
    public KafkaTestingHelper(
        IServiceProvider serviceProvider,
        ILogger<KafkaTestingHelper> logger)
        : base(serviceProvider, logger)
    {
        _topics = serviceProvider.GetService<IInMemoryTopicCollection>();
        _groups = serviceProvider.GetService<IMockedConsumerGroupsCollection>();
        _kafkaBroker = serviceProvider.GetRequiredService<KafkaBroker>();
    }

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
                string[] distinctBootstrapServers =
                    _kafkaBroker.Producers.Select(producer => ((KafkaProducerConfiguration)producer.Configuration).Client.BootstrapServers)
                        .Union(_kafkaBroker.Consumers.Select(consumer => ((KafkaConsumerConfiguration)consumer.Configuration).Client.BootstrapServers))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                if (distinctBootstrapServers.Length == 1)
                    return GetTopic(name, distinctBootstrapServers[0]);

                throw new InvalidOperationException($"Topic '{name}' not found and cannot be created on-the-fly. Try specifying the bootstrap servers.");
            case 0:
                return _topics.Get(name, bootstrapServers);
            default:
                return topics.Single();
        }
    }

    /// <inheritdoc cref="TestingHelper{TBroker}.WaitUntilAllMessagesAreConsumedCoreAsync(CancellationToken)" />
    protected override Task WaitUntilAllMessagesAreConsumedCoreAsync(CancellationToken cancellationToken) =>
        _groups == null
            ? Task.CompletedTask
            : Task.WhenAll(_groups.Select(group => @group.WaitUntilAllMessagesAreConsumedAsync(cancellationToken)));
}
