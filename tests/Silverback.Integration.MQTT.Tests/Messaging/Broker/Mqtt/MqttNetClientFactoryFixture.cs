// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using NSubstitute;
using Silverback.Messaging.Broker.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Broker.Mqtt;

public class MqttNetClientFactoryFixture
{
    [Fact]
    public void CreateClient_ShouldCreateClient()
    {
        MqttNetClientFactory factory = new(Substitute.For<IMqttNetLogger>());

        IMqttClient client = factory.CreateClient();

        client.Should().NotBeNull();
    }
}
