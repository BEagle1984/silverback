// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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

        using IServiceScope? scope = _serviceScopeFactory.CreateScope();
        MqttClientsInitializer? clientsInitializer = scope.ServiceProvider.GetService<MqttClientsInitializer>();

        if (clientsInitializer == null)
            throw new InvalidOperationException("The MqttClientsInitializer is not initialized.");

        MqttClientConfigurationBuilder builder = new();
        configurationBuilderAction.Invoke(builder);
        MqttClientConfiguration producerConfiguration = builder.Build();

        MqttProducer newProducer = clientsInitializer
            .InitializeProducers($"test-{Guid.NewGuid():N}", producerConfiguration, false)
            .Single();

        AsyncHelper.RunSynchronously(() => newProducer.Client.ConnectAsync());

        return newProducer;
    }

    /// <inheritdoc cref="ITestingHelper.GetProducerForEndpoint" />
    protected override IProducer? GetProducerForConsumer(IConsumerCollection consumers, string endpointName)
    {
        (MqttClientConfiguration? clientConfiguration, MqttConsumerEndpointConfiguration? endpointConfiguration) =
            consumers
                .OfType<MqttConsumer>()
                .SelectMany(
                    consumer => consumer.Configuration.ConsumerEndpoints.Select(
                        endpoint =>
                            (ConsumerConfiguration: consumer.Configuration, EndpointConfiguration: endpoint)))
                .FirstOrDefault(
                    tuple => tuple.EndpointConfiguration.RawName == endpointName || // TODO: strip $share?
                             tuple.EndpointConfiguration.FriendlyName == endpointName);

        if (clientConfiguration == null || endpointConfiguration == null)
            return null;

        return GetProducer(builder => MapConfiguration(builder, endpointConfiguration, clientConfiguration)); // TODO: Map other settings (e.g. encryption)
    }

    private static void MapConfiguration(
        MqttClientConfigurationBuilder builder,
        MqttConsumerEndpointConfiguration endpointConfiguration,
        MqttClientConfiguration clientConfiguration)
    {
        switch (clientConfiguration.Channel)
        {
            case MqttClientTcpConfiguration tcpChannel:
                builder.ConnectViaTcp(tcpChannel.Server!, tcpChannel.Port);

                if (tcpChannel.Tls.UseTls)
                    builder.EnableTls(tcpChannel.Tls);

                break;
            case MqttClientWebSocketConfiguration webSocketChannel:
                builder.ConnectViaWebSocket(webSocketChannel.Uri!);

                if (webSocketChannel.Tls.UseTls)
                    builder.EnableTls(webSocketChannel.Tls);

                break;
        }

        builder
            .WithClientId($"test-{Guid.NewGuid():N}")
            .Produce<object>(endpoint => MapEndpoint(endpoint, endpointConfiguration));
    }

    // TODO: Test all cases
    private static void MapEndpoint(
        MqttProducerEndpointConfigurationBuilder<object> endpoint,
        MqttConsumerEndpointConfiguration endpointConfiguration)
    {
        endpoint.SerializeUsing(endpointConfiguration.Serializer);

        string topic = endpointConfiguration.Topics.First(topic => topic == endpointConfiguration.RawName);

        endpoint.ProduceTo(topic);
    }
}
