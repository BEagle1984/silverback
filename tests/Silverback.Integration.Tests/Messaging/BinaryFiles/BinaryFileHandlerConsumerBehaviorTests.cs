// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.BinaryFiles
{
    public class BinaryFileHandlerConsumerBehaviorTests
    {
        [Fact]
        public async Task Handle_BinaryFileMessage_BinaryFileMessageReturned()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new[]
            {
                new MessageHeader("x-message-type", typeof(BinaryFileMessage).AssemblyQualifiedName),
            };
            var envelope = new RawInboundEnvelope(
                rawContent,
                headers,
                TestConsumerEndpoint.GetDefault(),
                "test");

            IRawInboundEnvelope? result = null;
            await new BinaryFileHandlerConsumerBehavior().Handle(
                new ConsumerPipelineContext(new[] { envelope }, Substitute.For<IConsumer>()),
                Substitute.For<IServiceProvider>(),
                (context, _) =>
                {
                    result = context.Envelopes.First();
                    return Task.CompletedTask;
                });

            result.Should().BeAssignableTo<IInboundEnvelope<BinaryFileMessage>>();
            var binaryFileMessage = result.As<IInboundEnvelope<BinaryFileMessage>>().Message!;
            binaryFileMessage.Content.Should().BeEquivalentTo(rawContent);
        }

        [Fact]
        public async Task Handle_NoBinaryFileHeaders_EnvelopeUntouched()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var envelope = new RawInboundEnvelope(
                rawContent,
                null,
                TestConsumerEndpoint.GetDefault(),
                "test");

            IRawInboundEnvelope? result = null;
            await new BinaryFileHandlerConsumerBehavior().Handle(
                new ConsumerPipelineContext(new[] { envelope }, Substitute.For<IConsumer>()),
                Substitute.For<IServiceProvider>(),
                (context, _) =>
                {
                    result = context.Envelopes.First();
                    return Task.CompletedTask;
                });

            result.Should().BeSameAs(envelope);
        }

        [Fact]
        public async Task Handle_EndpointWithBinaryFileMessageSerializer_EnvelopeUntouched()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new[]
            {
                new MessageHeader("x-message-type", typeof(BinaryFileMessage).AssemblyQualifiedName),
            };
            var endpoint = TestConsumerEndpoint.GetDefault();
            endpoint.Serializer = new BinaryFileMessageSerializer();
            var envelope = new RawInboundEnvelope(
                rawContent,
                headers,
                endpoint,
                "test");

            IRawInboundEnvelope? result = null;
            await new BinaryFileHandlerConsumerBehavior().Handle(
                new ConsumerPipelineContext(new[] { envelope }, Substitute.For<IConsumer>()),
                Substitute.For<IServiceProvider>(),
                (context, _) =>
                {
                    result = context.Envelopes.First();
                    return Task.CompletedTask;
                });

            result.Should().BeSameAs(envelope);
        }
    }
}
