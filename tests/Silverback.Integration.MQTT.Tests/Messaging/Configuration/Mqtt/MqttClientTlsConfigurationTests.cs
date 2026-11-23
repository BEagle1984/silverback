// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Security.Cryptography.X509Certificates;
using MQTTnet;
using Shouldly;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttClientTlsConfigurationTests
{
    [Fact]
    public void ToMqttNetType_ShouldMapTrustChain()
    {
        X509Certificate2Collection trustChain = [];
        MqttClientTlsConfiguration configuration = new()
        {
            TrustChain = trustChain
        };

        MqttClientTlsOptions options = configuration.ToMqttNetType();

        options.TrustChain.ShouldBeSameAs(trustChain);
    }
}
