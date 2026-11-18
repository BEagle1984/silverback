// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Testing;

/// <content>
///     Implements the <c>GetProducer</c> methods.
/// </content>
public partial class MqttTestingHelper
{
    /// <inheritdoc cref="IMqttTestingHelper.GetProducer" />
    public IProducer GetProducer(Action<MqttClientConfigurationBuilder> configurationBuilderAction)
    {
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        MqttClientsInitializer clientsInitializer = scope.ServiceProvider.GetService<MqttClientsInitializer>() ??
                                                    throw new InvalidOperationException("The MqttClientsInitializer is not initialized.");
        MqttClientConfigurationBuilder builder = new(_serviceProvider);
        configurationBuilderAction.Invoke(builder);
        MqttClientConfiguration producerConfiguration = builder.Build();

        MqttProducer newProducer = clientsInitializer
            .InitializeProducers($"test-{Guid.NewGuid():N}", producerConfiguration, false)
            .Single();

        newProducer.Client.ConnectAsync().SafeWait();

        return newProducer;
    }

    /// <inheritdoc cref="ITestingHelper.GetProducerForEndpoint" />
    protected override IProducer? GetProducerForConsumer(IConsumerCollection consumers, string endpointName)
    {
        Check.NotNull(endpointName, nameof(endpointName));

        List<(MqttClientConfiguration ClientConfiguration, MqttConsumerEndpointConfiguration EndpointConfiguration)> endpoints =
        [
            .. consumers
                .OfType<MqttConsumer>()
                .SelectMany(consumer => consumer.Configuration.ConsumerEndpoints.Select(endpoint => (consumer.Configuration, endpoint)))
        ];

        // The friendly name takes precedence over the topic name
        (MqttClientConfiguration? namedClientConfiguration, MqttConsumerEndpointConfiguration? namedEndpointConfiguration) =
            endpoints.FirstOrDefault(tuple => tuple.EndpointConfiguration.FriendlyName == endpointName);

        if (namedClientConfiguration == null || namedEndpointConfiguration == null)
        {
            (namedClientConfiguration, namedEndpointConfiguration) =
                endpoints.FirstOrDefault(tuple => tuple.EndpointConfiguration.RawName == endpointName);
        }

        if (namedClientConfiguration != null && namedEndpointConfiguration != null)
            return CreateProducerForNamedEndpoint(namedClientConfiguration, namedEndpointConfiguration, endpointName);

        // A wildcard filter can't be a topic name and no message can be published to it
        if (!IsWildcardFilter(endpointName))
        {
            string topic = GetTopicWithoutSharedSubscriptionGroup(endpointName);

            // A topic name is resolved to the endpoint subscribing it, giving precedence to the names over the wildcard filters
            (MqttClientConfiguration? clientConfiguration, MqttConsumerEndpointConfiguration? endpointConfiguration) =
                endpoints.FirstOrDefault(tuple => tuple.EndpointConfiguration.Topics.Any(subscribedTopic =>
                    GetTopicWithoutSharedSubscriptionGroup(subscribedTopic) == topic));

            if (clientConfiguration == null || endpointConfiguration == null)
            {
                (clientConfiguration, endpointConfiguration) =
                    endpoints.FirstOrDefault(tuple => tuple.EndpointConfiguration.Topics.Any(subscribedTopic =>
                        IsWildcardFilter(subscribedTopic) && new ParsedTopic(subscribedTopic).IsMatch(topic)));
            }

            if (clientConfiguration != null && endpointConfiguration != null)
                return CreateProducer(clientConfiguration, endpointConfiguration, topic);
        }

        if (IsWildcardFilter(endpointName))
        {
            throw new InvalidOperationException(
                $"Cannot produce to '{endpointName}', since it is a wildcard topic filter. " +
                "Get a producer for one of the matching topics instead, specifying its name.");
        }

        return null;
    }

    private static bool IsWildcardFilter(string topic) =>
        topic.Contains('+', StringComparison.Ordinal) || topic.Contains('#', StringComparison.Ordinal);

    // The messages are published to the actual topic, not to the '$share/{group}/{topic}' filter
    private static string GetTopicWithoutSharedSubscriptionGroup(string topic) =>
        ParsedTopic.TrySplitSharedSubscription(topic, out _, out string? actualTopic) ? actualTopic : topic;

    private static void MapConfiguration(
        MqttClientConfigurationBuilder builder,
        MqttConsumerEndpointConfiguration endpointConfiguration,
        MqttClientConfiguration clientConfiguration,
        string topic)
    {
        switch (clientConfiguration.Channel)
        {
            case MqttClientTcpConfiguration tcpChannel:
                builder.ConnectViaTcp(tcpChannel.RemoteEndpoint!);

                if (tcpChannel.Tls.UseTls)
                    builder.EnableTls();

                break;
            case MqttClientWebSocketConfiguration webSocketChannel:
                builder.ConnectViaWebSocket(webSocketChannel.Uri!);

                if (webSocketChannel.Tls.UseTls)
                    builder.EnableTls();

                break;
        }

        builder
            .WithClientId($"test-{Guid.NewGuid():N}")
            .UseProtocolVersion(clientConfiguration.ProtocolVersion)
            .DisableParallelProcessing()
            .Produce<object>(endpoint => MapEndpoint(endpoint, endpointConfiguration, topic));
    }

    private static void MapEndpoint(
        MqttProducerEndpointConfigurationBuilder<object> endpoint,
        MqttConsumerEndpointConfiguration endpointConfiguration,
        string topic)
    {
        endpoint.MirrorConsumerEndpoint(endpointConfiguration);
        endpoint.WithQualityOfServiceLevel(endpointConfiguration.QualityOfServiceLevel);
        endpoint.ProduceTo(topic);
    }

    private IProducer CreateProducerForNamedEndpoint(
        MqttClientConfiguration clientConfiguration,
        MqttConsumerEndpointConfiguration endpointConfiguration,
        string endpointName)
    {
        string? concreteTopic = endpointConfiguration.Topics
            .Select(GetTopicWithoutSharedSubscriptionGroup)
            .FirstOrDefault(topic => !IsWildcardFilter(topic));

        if (concreteTopic != null)
            return CreateProducer(clientConfiguration, endpointConfiguration, concreteTopic);

        // The friendly name can be a topic matching the endpoint's own wildcard filters, but it must not be resolved to another endpoint
        if (!IsWildcardFilter(endpointName))
        {
            string topic = GetTopicWithoutSharedSubscriptionGroup(endpointName);

            if (endpointConfiguration.Topics.Any(topicFilter => new ParsedTopic(topicFilter).IsMatch(topic)))
                return CreateProducer(clientConfiguration, endpointConfiguration, topic);
        }

        throw new InvalidOperationException(
            $"Cannot produce to the endpoint '{endpointConfiguration.DisplayName}', since it only subscribes wildcard topic " +
            "filters. Get a producer for one of the matching topics instead, specifying its name.");
    }

    private IProducer CreateProducer(
        MqttClientConfiguration clientConfiguration,
        MqttConsumerEndpointConfiguration endpointConfiguration,
        string topic) =>
        GetProducer(builder => MapConfiguration(builder, endpointConfiguration, clientConfiguration, topic));
}
