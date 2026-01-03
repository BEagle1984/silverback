// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using MQTTnet;
using MQTTnet.Diagnostics.Logger;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Broker.Mqtt;

public class MqttNetClientFactoryTests
{
    [Fact]
    public void CreateClient_ShouldCreateClient()
    {
        MqttNetClientFactory factory = new(Substitute.For<IMqttNetLogger>());

        IMqttClient client = factory.CreateClient();

        client.ShouldNotBeNull();
    }
}
