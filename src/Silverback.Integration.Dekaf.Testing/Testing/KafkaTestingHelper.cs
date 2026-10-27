// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dekaf.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Testing;

/// <inheritdoc cref="IKafkaTestingHelper" />
public class KafkaTestingHelper : TestingHelper, IKafkaTestingHelper
{
    private readonly IServiceProvider _serviceProvider;

    private readonly IServiceScopeFactory _serviceScopeFactory;

    private readonly InMemoryDekafClientsFactory? _clientsFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaTestingHelper" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ILogger{TCategoryName}" />.
    /// </param>
    public KafkaTestingHelper(IServiceProvider serviceProvider, ILogger<KafkaTestingHelper> logger)
        : this(serviceProvider, ResolveInMemoryClientsFactory(serviceProvider), logger)
    {
    }

    // Allows the tests to run the same helper against a real broker, overriding the consumer groups lookup
    private protected KafkaTestingHelper(IServiceProvider serviceProvider, InMemoryDekafClientsFactory? clientsFactory, ILogger<TestingHelper> logger)
        : base(serviceProvider, logger)
    {
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _clientsFactory = clientsFactory;
    }

    /// <inheritdoc cref="IKafkaTestingHelper.Cluster" />
    public InMemoryKafkaCluster Cluster =>
        _clientsFactory?.Cluster ?? throw new NotSupportedException("The in-memory cluster is not available, the clients are connected to a real broker.");

    /// <inheritdoc cref="IKafkaTestingHelper.ConsumerGroups" />
    public IReadOnlyCollection<IMockedConsumerGroup> ConsumerGroups =>
    [
        .. GetConsumerGroupIds()
            .Distinct(StringComparer.Ordinal)
            .Select(GetConsumerGroup)
    ];

    /// <inheritdoc cref="IKafkaTestingHelper.GetConsumerGroup" />
    public IMockedConsumerGroup GetConsumerGroup(string groupId) => GetConsumerGroupCore(groupId);

    /// <inheritdoc cref="IKafkaTestingHelper.GetProducer" />
    public IProducer GetProducer(Action<KafkaProducerConfigurationBuilder> configurationBuilderAction)
    {
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        KafkaProducersInitializer producersInitializer = scope.ServiceProvider.GetService<KafkaProducersInitializer>() ??
                                                         throw new InvalidOperationException("The KafkaProducersInitializer is not initialized.");

        KafkaProducerConfigurationBuilder builder = new(_serviceProvider);
        configurationBuilderAction.Invoke(builder);
        KafkaProducerConfiguration producerConfiguration = builder.Build();

        IProducer newProducer = producersInitializer
            .InitializeProducers($"test-{Guid.NewGuid():N}", producerConfiguration, false)
            .Single();

        // The method is synchronous for compatibility with the Kafka.Testing helper, the connection is quick
        if (newProducer is KafkaProducer kafkaProducer)
            kafkaProducer.Client.ConnectAsync().SafeWait();

        return newProducer;
    }

    /// <inheritdoc cref="IKafkaTestingHelper.WaitUntilAllMessagesAreCommittedAsync(string[])" />
    public ValueTask WaitUntilAllMessagesAreCommittedAsync(params string[] endpointNames) =>
        WaitUntilAllMessagesAreCommittedWithTimeoutAsync(true, null, endpointNames);

    /// <inheritdoc cref="IKafkaTestingHelper.WaitUntilAllMessagesAreCommittedAsync(bool,TimeSpan?)" />
    public ValueTask WaitUntilAllMessagesAreCommittedAsync(bool throwTimeoutException, TimeSpan? timeout = null) =>
        WaitUntilAllMessagesAreCommittedWithTimeoutAsync(throwTimeoutException, timeout, []);

    /// <inheritdoc cref="IKafkaTestingHelper.WaitUntilAllMessagesAreCommittedAsync(bool,CancellationToken,string[])" />
    public ValueTask WaitUntilAllMessagesAreCommittedAsync(bool throwTimeoutException, CancellationToken cancellationToken, params string[] endpointNames) =>
        WaitUntilAllMessagesCoreAsync(
            (names, token) => WaitForAllGroupsAsync(names, true, token),
            "Timeout elapsed before all message offsets could be committed",
            throwTimeoutException,
            endpointNames,
            cancellationToken);

    /// <inheritdoc cref="TestingHelper.WaitUntilAllMessagesAreConsumedCoreAsync" />
    protected override Task WaitUntilAllMessagesAreConsumedCoreAsync(IReadOnlyCollection<string> endpointNames, CancellationToken cancellationToken) =>
        WaitForAllGroupsAsync(endpointNames, false, cancellationToken);

    /// <inheritdoc cref="TestingHelper.GetProducerForConsumer" />
    protected override IProducer? GetProducerForConsumer(IConsumerCollection consumers, string endpointName)
    {
        (KafkaConsumerConfiguration? consumerConfiguration, KafkaConsumerEndpointConfiguration? endpointConfiguration) =
            consumers
                .OfType<IKafkaConsumer>()
                .SelectMany(consumer => consumer.Configuration.Endpoints.Select(endpoint =>
                    (ConsumerConfiguration: consumer.Configuration, EndpointConfiguration: endpoint)))
                .FirstOrDefault(tuple => tuple.EndpointConfiguration.RawName == endpointName ||
                                         tuple.EndpointConfiguration.FriendlyName == endpointName);

        if (consumerConfiguration == null || endpointConfiguration == null)
            return null;

        return GetProducer(producer => producer
            .WithBootstrapServers(consumerConfiguration.BootstrapServers)
            .Produce<object>(endpoint => endpoint
                .SerializeUsing(endpointConfiguration.Deserializer.GetCompatibleSerializer())
                .ProduceTo(endpointConfiguration.TopicPartitions.First().TopicPartition)));
    }

    private protected virtual IEnumerable<string> GetConsumerGroupIds() =>
        GetInMemoryClientsFactory().Consumers
            .Select(consumer => consumer.GroupId)
            .OfType<string>();

    private protected virtual IMockedConsumerGroup GetConsumerGroupCore(string groupId) => new MockedConsumerGroup(groupId, GetInMemoryClientsFactory());

    private static InMemoryDekafClientsFactory ResolveInMemoryClientsFactory(IServiceProvider serviceProvider) =>
        Check.NotNull(serviceProvider, nameof(serviceProvider)).GetRequiredService<IDekafClientsFactory>() as InMemoryDekafClientsFactory ??
        throw new InvalidOperationException("The in-memory clients are not registered. Call AddMockedKafka or UseMockedKafka.");

    private InMemoryDekafClientsFactory GetInMemoryClientsFactory() =>
        _clientsFactory ?? throw new InvalidOperationException("The in-memory clients are not registered.");

    private async ValueTask WaitUntilAllMessagesAreCommittedWithTimeoutAsync(bool throwTimeoutException, TimeSpan? timeout, string[] endpointNames)
    {
        using CancellationTokenSource cancellationTokenSource = new(timeout ?? DefaultWaitTimeout);
        await WaitUntilAllMessagesAreCommittedAsync(throwTimeoutException, cancellationTokenSource.Token, endpointNames).ConfigureAwait(false);
    }

    private Task WaitForAllGroupsAsync(IReadOnlyCollection<string> endpointNames, bool waitForCommit, CancellationToken cancellationToken)
    {
        string[] topicNames = [.. endpointNames.Select(GetEndpointRawName)];

        return Task.WhenAll(
            ConsumerGroups.Select(group => waitForCommit
                ? group.WaitUntilAllMessagesAreCommittedAsync(topicNames, cancellationToken).AsTask()
                : group.WaitUntilAllMessagesAreConsumedAsync(topicNames, cancellationToken).AsTask()));
    }
}
