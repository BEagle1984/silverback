// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet.Protocol;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public partial class MqttProducerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void SetRe()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);

        MqttProducerEndpointConfiguration configuration = builder.Build();
        configuration.QualityOfServiceLevel.ShouldBe(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    [Fact]
    public void WithAtMostOnceQoS_ShouldSetQualityOfServiceLevel()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic").WithAtMostOnceQoS();

        MqttProducerEndpointConfiguration configuration = builder.Build();
        configuration.QualityOfServiceLevel.ShouldBe(MqttQualityOfServiceLevel.AtMostOnce);
    }

    [Fact]
    public void WithAtLeastOnceQoS_ShouldSetQualityOfServiceLevel()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic").WithAtLeastOnceQoS();

        MqttProducerEndpointConfiguration configuration = builder.Build();
        configuration.QualityOfServiceLevel.ShouldBe(MqttQualityOfServiceLevel.AtLeastOnce);
    }
}
