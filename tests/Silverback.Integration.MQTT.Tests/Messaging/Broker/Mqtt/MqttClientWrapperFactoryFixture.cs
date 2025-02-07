// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using MQTTnet;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Broker.Mqtt;

public class MqttClientWrapperFactoryFixture
{
    [Fact]
    public void Create_ShouldCreateWrapper()
    {
        IMqttNetClientFactory clientFactory = Substitute.For<IMqttNetClientFactory>();
        clientFactory.CreateClient().Returns(Substitute.For<IMqttClient>());
        IBrokerClientCallbacksInvoker callbacksInvoker = Substitute.For<IBrokerClientCallbacksInvoker>();
        ISilverbackLoggerFactory loggerFactory = new SilverbackLoggerFactorySubstitute();
        MqttClientWrapperFactory factory = new(clientFactory, callbacksInvoker, loggerFactory);
        MqttClientConfiguration clientConfiguration = new();

        IMqttClientWrapper wrapper = factory.Create("test", clientConfiguration);

        wrapper.Name.Should().Be("test");
        wrapper.Configuration.Should().BeSameAs(clientConfiguration);
    }
}
