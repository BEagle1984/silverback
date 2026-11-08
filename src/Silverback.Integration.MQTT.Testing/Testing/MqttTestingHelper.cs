// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Testing;

/// <inheritdoc cref="IMqttTestingHelper" />
public partial class MqttTestingHelper : TestingHelper, IMqttTestingHelper
{
    private readonly IServiceProvider _serviceProvider;

    private readonly IInMemoryMqttBroker? _inMemoryMqttBroker;

    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttTestingHelper" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger" />.
    /// </param>
    public MqttTestingHelper(
        IServiceProvider serviceProvider,
        ILogger<MqttTestingHelper> logger)
        : base(serviceProvider, logger)
    {
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _inMemoryMqttBroker = serviceProvider.GetService<IInMemoryMqttBroker>();
        _serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
    }

    /// <inheritdoc cref="IMqttTestingHelper.GetClientSession" />
    public IClientSession GetClientSession(string clientId)
    {
        if (_inMemoryMqttBroker == null)
            throw new InvalidOperationException("The IInMemoryMqttBroker is not initialized.");

        return _inMemoryMqttBroker.GetClientSession(clientId);
    }

    /// <inheritdoc cref="IMqttTestingHelper.GetMessages" />
    public IReadOnlyList<MqttApplicationMessage> GetMessages(string topic)
    {
        if (_inMemoryMqttBroker == null)
            throw new InvalidOperationException("The IInMemoryMqttBroker is not initialized.");

        return _inMemoryMqttBroker.GetMessages(topic);
    }

    /// <inheritdoc cref="TestingHelper.WaitUntilAllMessagesAreConsumedCoreAsync(IReadOnlyCollection{string},CancellationToken)" />
    protected override Task WaitUntilAllMessagesAreConsumedCoreAsync(IReadOnlyCollection<string> endpointNames, CancellationToken cancellationToken)
    {
        Check.NotNull(endpointNames, nameof(endpointNames));

        if (_inMemoryMqttBroker == null)
            return Task.CompletedTask;

        return _inMemoryMqttBroker.WaitUntilAllMessagesAreConsumedAsync(
            [.. endpointNames.SelectMany(GetTopicFilters)],
            cancellationToken);
    }

    // A consumer endpoint can subscribe multiple topics, wildcard or shared subscriptions, which are waited for separately
    private IEnumerable<string> GetTopicFilters(string endpointName)
    {
        if (GetProducerEndpointConfiguration(endpointName) is { } producerEndpointConfiguration)
            return [producerEndpointConfiguration.RawName];

        ConsumerEndpointConfiguration? consumerEndpointConfiguration = GetConsumerEndpointConfiguration(endpointName);

        // The endpoint of another broker doesn't match any MQTT topic
        if (consumerEndpointConfiguration != null)
        {
            return consumerEndpointConfiguration is MqttConsumerEndpointConfiguration mqttConsumerEndpointConfiguration
                ? mqttConsumerEndpointConfiguration.Topics
                : [consumerEndpointConfiguration.RawName];
        }

        if (IsSubscribedTopic(endpointName))
            return [endpointName];

        throw new InvalidOperationException($"Endpoint '{endpointName}' not found.");
    }

    // The topic (or filter) is subscribed by one of the consumers, or the topic matches one of the subscribed filters
    private bool IsSubscribedTopic(string topic) =>
        _serviceProvider.GetService<IConsumerCollection>()?
            .OfType<MqttConsumer>()
            .Any(consumer => consumer.EndpointsConfiguration.Any(endpoint => endpoint.Topics.Any(subscribedTopic =>
                subscribedTopic == topic || new ParsedTopic(subscribedTopic).IsMatch(topic)))) ?? false;
}
