// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Subscribers
{
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
            var inboundEnvelope = new InboundEnvelope(
                new MemoryStream(),
                new List<MessageHeader>(),
                new TestOffset(),
                new MqttConsumerEndpoint("my-topic")
                {
                    Configuration =
                    {
                        ClientId = envelopeClientId
                    }
                },
                "my-topic");

            var result = new MqttClientIdFilterAttribute("client1", "client2").MustProcess(inboundEnvelope);

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void MustProcess_NonInboundEnvelope_FalseIsReturned()
        {
            var result = new MqttClientIdFilterAttribute().MustProcess(new TestEventOne());

            result.Should().BeFalse();
        }

        [Fact]
        public void MustProcess_InboundEnvelopeWithNonMqttEndpoint_FalseIsReturned()
        {
            var inboundEnvelope = new InboundEnvelope(
                new MemoryStream(),
                new List<MessageHeader>(),
                new TestOffset(),
                new SomeConsumerEndpoint("test"),
                string.Empty);

            var result = new MqttClientIdFilterAttribute().MustProcess(inboundEnvelope);

            result.Should().BeFalse();
        }

        private class SomeConsumerEndpoint : ConsumerEndpoint
        {
            public SomeConsumerEndpoint(string name)
                : base(name)
            {
            }

            public override string GetUniqueConsumerGroupName() => Name;
        }
    }
}
