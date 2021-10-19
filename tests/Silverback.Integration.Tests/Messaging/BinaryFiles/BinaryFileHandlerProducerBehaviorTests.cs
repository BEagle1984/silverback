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
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.BinaryFiles
{
    public class BinaryFileHandlerProducerBehaviorTests
    {
        [Fact]
        public async Task HandleAsync_BinaryFileMessage_RawContentProduced()
        {
            BinaryFileMessage message = new() { Content = BytesUtil.GetRandomStream() };
            OutboundEnvelope envelope = new(message, null, TestProducerEndpoint.GetDefault());

            IOutboundEnvelope? result = null;
            await new BinaryFileHandlerProducerBehavior().HandleAsync(
                new ProducerPipelineContext(
                    envelope,
                    Substitute.For<IProducer>(),
                    Substitute.For<IServiceProvider>()),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            result.Should().NotBeNull();
            result!.RawMessage.Should().BeSameAs(message.Content);
        }

        [Fact]
        public async Task HandleAsync_InheritedBinaryFileMessage_RawContentProduced()
        {
            InheritedBinaryFileMessage message = new() { Content = BytesUtil.GetRandomStream() };
            OutboundEnvelope envelope = new(message, null, TestProducerEndpoint.GetDefault());

            IOutboundEnvelope? result = null;
            await new BinaryFileHandlerProducerBehavior().HandleAsync(
                new ProducerPipelineContext(
                    envelope,
                    Substitute.For<IProducer>(),
                    Substitute.For<IServiceProvider>()),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            result.Should().NotBeNull();
            result!.RawMessage.Should().BeSameAs(message.Content);
        }

        [Fact]
        public async Task HandleAsync_NonBinaryFileMessage_EnvelopeUntouched()
        {
            BinaryFileMessage message = new() { Content = BytesUtil.GetRandomStream() };
            TestProducerConfiguration endpointConfiguration = new("test")
            {
                Serializer = new BinaryFileMessageSerializer()
            };
            OutboundEnvelope envelope = new(message, null, endpointConfiguration.GetDefaultEndpoint());

            IOutboundEnvelope? result = null;
            await new BinaryFileHandlerProducerBehavior().HandleAsync(
                new ProducerPipelineContext(
                    envelope,
                    Substitute.For<IProducer>(),
                    Substitute.For<IServiceProvider>()),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            result.Should().NotBeNull();
            result!.Should().BeSameAs(envelope);
        }

        [Fact]
        public async Task HandleAsync_EndpointWithBinaryFileMessageSerializer_EnvelopeUntouched()
        {
            TestEventOne message = new() { Content = "hey!" };
            OutboundEnvelope envelope = new(message, null, TestProducerEndpoint.GetDefault());

            IOutboundEnvelope? result = null;
            await new BinaryFileHandlerProducerBehavior().HandleAsync(
                new ProducerPipelineContext(envelope, Substitute.For<IProducer>(), Substitute.For<IServiceProvider>()),
                context =>
                {
                    result = context.Envelope;
                    return Task.CompletedTask;
                });

            result.Should().NotBeNull();
            result!.Should().BeSameAs(envelope);
        }

        private sealed class InheritedBinaryFileMessage : BinaryFileMessage
        {
        }
    }
}
