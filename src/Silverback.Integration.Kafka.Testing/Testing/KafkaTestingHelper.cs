// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Util;

namespace Silverback.Testing;

/// <inheritdoc cref="IKafkaTestingHelper" />
public partial class KafkaTestingHelper : TestingHelper, IKafkaTestingHelper
{
    private readonly IServiceProvider _serviceProvider;

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
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _topics = serviceProvider.GetService<IInMemoryTopicCollection>();
        _groups = serviceProvider.GetService<IMockedConsumerGroupsCollection>();
        _serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
    }

    /// <inheritdoc cref="IKafkaTestingHelper.ConsumerGroups" />
    public IReadOnlyCollection<IMockedConsumerGroup> ConsumerGroups => (IReadOnlyCollection<IMockedConsumerGroup>?)_groups ?? [];

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

        List<IInMemoryTopic> topics =
        [
            .. _topics.Where(topic =>
                topic.Name == name &&
                (bootstrapServers == null || string.Equals(
                    bootstrapServers,
                    topic.BootstrapServers,
                    StringComparison.OrdinalIgnoreCase)))
        ];

        switch (topics.Count)
        {
            case > 1:
                throw new InvalidOperationException($"More than one topic '{name}' found. Try specifying the bootstrap servers.");
            case 0 when bootstrapServers == null:
                string[] distinctBootstrapServers = [.. _topics.Select(topic => topic.BootstrapServers).Distinct()];

                if (distinctBootstrapServers.Length == 1)
                    return GetTopic(name, distinctBootstrapServers[0]);

                throw new InvalidOperationException($"Topic '{name}' not found and cannot be created on-the-fly. Try specifying the bootstrap servers.");
            case 0:
                return _topics.Get(name, bootstrapServers);
            default:
                // Get it through the collection, to wait for the notification of a topic being created concurrently
                return _topics.Get(topics[0].Name, topics[0].BootstrapServers);
        }
    }

    /// <inheritdoc cref="TestingHelper.WaitUntilAllMessagesAreConsumedCoreAsync(IReadOnlyCollection{string},CancellationToken)" />
    protected override Task WaitUntilAllMessagesAreConsumedCoreAsync(IReadOnlyCollection<string> endpointNames, CancellationToken cancellationToken)
    {
        Check.NotNull(endpointNames, nameof(endpointNames));

        if (_groups == null)
            return Task.CompletedTask;

        string[] topicNames = [.. endpointNames.SelectMany(GetTopicNames)];

        return Task.WhenAll(
            _groups.Select(group => group.WaitUntilAllMessagesAreConsumedAsync(topicNames, cancellationToken).AsTask()));
    }

    // A consumer endpoint can subscribe multiple topics and patterns, which are waited for separately
    private IEnumerable<string> GetTopicNames(string endpointName)
    {
        ProducerEndpointConfiguration? producerEndpointConfiguration = GetProducerEndpointConfiguration(endpointName);

        if (producerEndpointConfiguration != null)
        {
            return producerEndpointConfiguration.EndpointResolver is KafkaStaticProducerEndpointResolver staticEndpointResolver
                ? [staticEndpointResolver.TopicPartition.Topic]
                : [producerEndpointConfiguration.RawName];
        }

        ConsumerEndpointConfiguration? consumerEndpointConfiguration = GetConsumerEndpointConfiguration(endpointName);

        // The endpoint of another broker doesn't match any Kafka topic
        if (consumerEndpointConfiguration != null)
        {
            return consumerEndpointConfiguration is KafkaConsumerEndpointConfiguration kafkaConsumerEndpointConfiguration
                ? kafkaConsumerEndpointConfiguration.TopicPartitions.Select(topicPartition => topicPartition.Topic).Distinct()
                : [consumerEndpointConfiguration.RawName];
        }

        if (IsSubscribedTopic(endpointName))
            return [endpointName];

        throw new InvalidOperationException($"Endpoint '{endpointName}' not found.");
    }

    // The topic (or pattern) is subscribed by one of the consumers, or the topic matches one of the subscribed patterns (a pattern can't
    // be a topic name)
    private bool IsSubscribedTopic(string topic) =>
        _serviceProvider.GetService<IConsumerCollection>()?
            .OfType<IKafkaConsumer>()
            .Any(consumer => consumer.Configuration.Endpoints.Any(endpoint => endpoint.TopicPartitions.Any(topicPartition =>
                topicPartition.Topic == topic ||
                !KafkaTopicPattern.IsPattern(topic) &&
                KafkaTopicPattern.IsMatch(
                    topicPartition.Topic,
                    topic,
                    KafkaTopicPattern.IsFullMatchRequired(consumer.Configuration.GroupProtocol))))) ?? false;
}
