// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.Routing
{
    public class OutboundEnvelopeFactoryTests
    {
        [Fact]
        public void CreateOutboundEnvelope_NullMessage_UntypedEnvelopeReturned()
        {
            var endpoint = TestProducerEndpoint.GetDefault();
            var headers = new[] { new MessageHeader("one", "1"), new MessageHeader("two", "2") };

            var factory = new OutboundEnvelopeFactory(
                new OutboundRoutingConfiguration
                {
                    PublishOutboundMessagesToInternalBus = true
                });

            var envelope = factory.CreateOutboundEnvelope(
                null,
                headers,
                TestProducerEndpoint.GetDefault());

            envelope.Should().BeOfType<OutboundEnvelope>();
            envelope.Message.Should().BeNull();
            envelope.Headers.Should().ContainSingle(header => header.Name == "one" && header.Value == "1");
            envelope.Headers.Should().ContainSingle(header => header.Name == "two" && header.Value == "2");
            envelope.Endpoint.Should().Be(endpoint);
        }

        [Fact]
        public void CreateOutboundEnvelope_NotNullMessage_TypedEnvelopeReturned()
        {
            var endpoint = TestProducerEndpoint.GetDefault();
            var message = new TestEventOne();
            var headers = new[] { new MessageHeader("one", "1"), new MessageHeader("two", "2") };

            var factory = new OutboundEnvelopeFactory(
                new OutboundRoutingConfiguration
                {
                    PublishOutboundMessagesToInternalBus = true
                });

            var envelope = factory.CreateOutboundEnvelope(
                message,
                headers,
                TestProducerEndpoint.GetDefault());

            envelope.Should().BeOfType<OutboundEnvelope<TestEventOne>>();
            envelope.As<OutboundEnvelope<TestEventOne>>().Message.Should().Be(message);
            envelope.Headers.Should().ContainSingle(header => header.Name == "one" && header.Value == "1");
            envelope.Headers.Should().ContainSingle(header => header.Name == "two" && header.Value == "2");
            envelope.Endpoint.Should().Be(endpoint);
        }
    }
}
