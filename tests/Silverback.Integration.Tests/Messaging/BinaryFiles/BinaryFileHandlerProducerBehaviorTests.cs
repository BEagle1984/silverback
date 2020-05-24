// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.BinaryFiles
{
    public class BinaryFileHandlerProducerBehaviorTests
    {
        [Fact]
        public async Task Handle_BinaryFileMessage_RawContentProduced()
        {
            var message = new BinaryFileMessage
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }
            };
            var envelope = new OutboundEnvelope(message, null, TestProducerEndpoint.GetDefault());

            IOutboundEnvelope? result = null;
            await new BinaryFileHandlerProducerBehavior().Handle(
                new ProducerPipelineContext(envelope, Substitute.For<IProducer>()),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            result.Should().NotBeNull();
            result!.RawMessage.Should().BeEquivalentTo(message.Content);
        }

        [Fact]
        public async Task Handle_InheritedBinaryFileMessage_RawContentProduced()
        {
            var message = new InheritedBinaryFileMessage()
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }
            };
            var envelope = new OutboundEnvelope(message, null, TestProducerEndpoint.GetDefault());

            IOutboundEnvelope? result = null;
            await new BinaryFileHandlerProducerBehavior().Handle(
                new ProducerPipelineContext(envelope, Substitute.For<IProducer>()),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            result.Should().NotBeNull();
            result!.RawMessage.Should().BeEquivalentTo(message.Content);
        }

        [Fact]
        public async Task Handle_NonBinaryFileMessage_EnvelopeUntouched()
        {
            var message = new BinaryFileMessage
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }
            };
            var endpoint = TestProducerEndpoint.GetDefault();
            endpoint.Serializer = new BinaryFileMessageSerializer();
            var envelope = new OutboundEnvelope(message, null, endpoint);

            IOutboundEnvelope? result = null;
            await new BinaryFileHandlerProducerBehavior().Handle(
                new ProducerPipelineContext(envelope, Substitute.For<IProducer>()),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            result.Should().NotBeNull();
            result!.Should().BeSameAs(envelope);
        }

        [Fact]
        public async Task Handle_EndpointWithBinaryFileMessageSerializer_EnvelopeUntouched()
        {
            var message = new TestEventOne
            {
                Content = "hey!"
            };
            var envelope = new OutboundEnvelope(message, null, TestProducerEndpoint.GetDefault());

            IOutboundEnvelope? result = null;
            await new BinaryFileHandlerProducerBehavior().Handle(
                new ProducerPipelineContext(envelope, Substitute.For<IProducer>()),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            result.Should().NotBeNull();
            result!.Should().BeSameAs(envelope);
        }

        private class InheritedBinaryFileMessage : BinaryFileMessage
        {
        }
    }
}