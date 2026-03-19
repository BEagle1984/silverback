// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Testing.Messaging.Messages;

public class InboundEnvelopeBuilderExtensionsTests
{
    [Fact]
    public void WithMqttCorrelationData_ShouldSetCorrelationData()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        byte[] correlationData = [1, 2, 3];

        builder.WithMqttCorrelationData(correlationData);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.GetMqttCorrelationData().ShouldBe(correlationData);
    }

    [Fact]
    public void WithMqttTopic_ShouldSetTopic()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();

        builder.WithMqttTopic("my/topic");

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        MqttConsumerEndpoint mqttEndpoint = envelope.Endpoint.ShouldBeOfType<MqttConsumerEndpoint>();
        mqttEndpoint.Topic.ShouldBe("my/topic");
        mqttEndpoint.Configuration.ShouldBeOfType<MqttConsumerEndpointConfiguration>();
    }
}
