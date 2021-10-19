// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.BinaryFiles
{
    public class BinaryFileHandlerConsumerBehaviorTests
    {
        [Fact]
        public async Task HandleAsync_BinaryFileMessage_BinaryFileMessageReturned()
        {
            byte[] rawContent = BytesUtil.GetRandomBytes();
            MessageHeader[] headers =
            {
                new MessageHeader("x-message-type", typeof(BinaryFileMessage).AssemblyQualifiedName)
            };
            RawInboundEnvelope envelope = new(
                rawContent,
                headers,
                TestConsumerEndpoint.GetDefault(),
                new TestOffset());

            IRawInboundEnvelope? result = null;
            await new BinaryFileHandlerConsumerBehavior().HandleAsync(
                new ConsumerPipelineContext(
                    envelope,
                    Substitute.For<IConsumer>(),
                    Substitute.For<ISequenceStore>(),
                    Substitute.For<IServiceProvider>()),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            result.Should().BeAssignableTo<IInboundEnvelope<BinaryFileMessage>>();
            BinaryFileMessage binaryFileMessage = result.As<IInboundEnvelope<BinaryFileMessage>>().Message!;
            binaryFileMessage.Content.ReadAll().Should().BeEquivalentTo(rawContent);
        }

        [Fact]
        public async Task HandleAsync_NoBinaryFileHeaders_EnvelopeUntouched()
        {
            byte[] rawContent = BytesUtil.GetRandomBytes();
            RawInboundEnvelope envelope = new(
                rawContent,
                null,
                TestConsumerEndpoint.GetDefault(),
                new TestOffset());

            IRawInboundEnvelope? result = null;
            await new BinaryFileHandlerConsumerBehavior().HandleAsync(
                new ConsumerPipelineContext(
                    envelope,
                    Substitute.For<IConsumer>(),
                    Substitute.For<ISequenceStore>(),
                    Substitute.For<IServiceProvider>()),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            result.Should().BeSameAs(envelope);
        }

        [Fact]
        public async Task HandleAsync_EndpointWithBinaryFileMessageSerializer_EnvelopeUntouched()
        {
            byte[] rawContent = BytesUtil.GetRandomBytes();
            MessageHeader[] headers =
            {
                new MessageHeader("x-message-type", typeof(BinaryFileMessage).AssemblyQualifiedName)
            };
            TestConsumerConfiguration endpointConfiguration = new("test")
            {
                Serializer = new BinaryFileMessageSerializer()
            };

            RawInboundEnvelope envelope = new(
                rawContent,
                headers,
                endpointConfiguration.GetDefaultEndpoint(),
                new TestOffset());

            IRawInboundEnvelope? result = null;
            await new BinaryFileHandlerConsumerBehavior().HandleAsync(
                new ConsumerPipelineContext(
                    envelope,
                    Substitute.For<IConsumer>(),
                    Substitute.For<ISequenceStore>(),
                    Substitute.For<IServiceProvider>()),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            result.Should().BeSameAs(envelope);
        }
    }
}
