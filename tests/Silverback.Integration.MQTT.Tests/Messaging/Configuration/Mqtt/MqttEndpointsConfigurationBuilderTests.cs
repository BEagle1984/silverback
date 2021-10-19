// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Logging;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttEndpointsConfigurationBuilderTests
{
    [Fact]
    public async Task AddInbound_WithoutMessageType_DefaultSerializerSet()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(broker => broker.AddMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(
                            config => config
                                .WithClientId("test")
                                .ConnectViaTcp("mqtt-broker"))
                        .AddInbound(
                            endpoint => endpoint
                                .ConsumeFrom("test"))));

        MqttBroker broker = serviceProvider.GetRequiredService<MqttBroker>();

        await broker.ConnectAsync();

        broker.Consumers[0].Configuration.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
    }

    [Fact]
    public async Task AddInbound_WithMessageTypeGenericParameter_TypedDefaultSerializerSet()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(broker => broker.AddMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(
                            config => config
                                .WithClientId("test")
                                .ConnectViaTcp("mqtt-broker"))
                        .AddInbound<TestEventOne>(
                            endpoint => endpoint
                                .ConsumeFrom("test"))));

        MqttBroker broker = serviceProvider.GetRequiredService<MqttBroker>();

        await broker.ConnectAsync();

        broker.Consumers[0].Configuration.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }
}
