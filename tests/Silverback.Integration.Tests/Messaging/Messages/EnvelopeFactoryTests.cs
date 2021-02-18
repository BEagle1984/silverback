// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages
{
    public class EnvelopeFactoryTests
    {
        [Fact]
        public void Create_OutboundEnvelope_Created()
        {
            var envelope = EnvelopeFactory.Create(
                new TestEventOne(),
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                TestProducerEndpoint.GetDefault());

            envelope.Should().NotBeNull();
            envelope.Should().BeOfType<OutboundEnvelope<TestEventOne>>();
            envelope.Message.Should().BeOfType<TestEventOne>();
            envelope.Headers.Should().HaveCount(2);
            envelope.Endpoint.Should().BeOfType<TestProducerEndpoint>();
        }

        [Fact]
        public void Create_RawInboundEnvelopeFromByteArray_Created()
        {
            var envelope = EnvelopeFactory.Create(
                Array.Empty<byte>(),
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                TestConsumerEndpoint.GetDefault(),
                new TestOffset());

            envelope.Should().NotBeNull();
            envelope.Should().BeOfType<RawInboundEnvelope>();
            envelope.RawMessage.Should().NotBeNull();
            envelope.Headers.Should().HaveCount(2);
            envelope.Endpoint.Should().BeOfType<TestConsumerEndpoint>();
        }

        [Fact]
        public void Create_RawInboundEnvelopeFromStream_Created()
        {
            var envelope = EnvelopeFactory.Create(
                new MemoryStream(),
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                TestConsumerEndpoint.GetDefault(),
                new TestOffset());

            envelope.Should().NotBeNull();
            envelope.Should().BeOfType<RawInboundEnvelope>();
            envelope.RawMessage.Should().NotBeNull();
            envelope.Headers.Should().HaveCount(2);
            envelope.Endpoint.Should().BeOfType<TestConsumerEndpoint>();
        }

        [Fact]
        public void Create_InboundEnvelopeFromStream_Created()
        {
            var rawEnvelope = EnvelopeFactory.Create(
                new MemoryStream(),
                new MessageHeaderCollection
                {
                    { "one", "1" }, { "two", "2" }
                },
                TestConsumerEndpoint.GetDefault(),
                new TestOffset());

            var envelope = EnvelopeFactory.Create(new TestEventOne(), rawEnvelope);

            envelope.Should().NotBeNull();
            envelope.Should().BeOfType<InboundEnvelope<TestEventOne>>();
            envelope.RawMessage.Should().NotBeNull();
            envelope.Headers.Should().HaveCount(2);
            envelope.Endpoint.Should().BeOfType<TestConsumerEndpoint>();
        }
    }
}
