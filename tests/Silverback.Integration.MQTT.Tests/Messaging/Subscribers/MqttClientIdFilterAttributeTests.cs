// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Subscribers;

public class MqttClientIdFilterAttributeTests
{
    [Theory]
    [InlineData("client1", true)]
    [InlineData("client2", true)]
    [InlineData("client3", false)]
    public void MustProcess_InboundEnvelopesWithDifferentClientId_ExpectedResultIsReturned(
        string envelopeClientId,
        bool expectedResult)
    {
        InboundEnvelope inboundEnvelope = new(
            new MemoryStream(),
            new List<MessageHeader>(),
            new TestOffset(),
            new MqttConsumerEndpoint(
                "my-topic",
                new MqttConsumerConfiguration
                {
                    Client = new MqttClientConfiguration
                    {
                        ClientId = envelopeClientId
                    }
                }));

        bool result = new MqttClientIdFilterAttribute("client1", "client2").MustProcess(inboundEnvelope);

        result.Should().Be(expectedResult);
    }

    [Fact]
    public void MustProcess_NonInboundEnvelope_FalseIsReturned()
    {
        bool result = new MqttClientIdFilterAttribute().MustProcess(new TestEventOne());

        result.Should().BeFalse();
    }

    [Fact]
    public void MustProcess_InboundEnvelopeWithNonMqttEndpoint_FalseIsReturned()
    {
        InboundEnvelope inboundEnvelope = new(
            new MemoryStream(),
            new List<MessageHeader>(),
            new TestOffset(),
            new SomeConsumerEndpoint(new SomeConsumerConfiguration()));

        bool result = new MqttClientIdFilterAttribute().MustProcess(inboundEnvelope);

        result.Should().BeFalse();
    }

    private sealed record SomeConsumerConfiguration : ConsumerConfiguration
    {
        public override string GetUniqueConsumerGroupName() => throw new NotImplementedException();
    }

    private sealed record SomeConsumerEndpoint : ConsumerEndpoint<SomeConsumerConfiguration>
    {
        public SomeConsumerEndpoint(SomeConsumerConfiguration endpoint)
            : base("test", endpoint)
        {
        }
    }
}
