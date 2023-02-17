// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.BinaryMessages;

public class BinaryMessageHandlerProducerBehaviorTests
{
    [Fact]
    public async Task HandleAsync_BinaryMessage_RawContentProduced()
    {
        BinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        OutboundEnvelope envelope = new(message, null, TestProducerEndpoint.GetDefault(), Substitute.For<IProducer>());

        IOutboundEnvelope? result = null;
        await new BinaryMessageHandlerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                Substitute.For<IProducer>(),
                Substitute.For<IServiceProvider>()),
            context =>
            {
                result = context.Envelope;
                return default;
            });

        result.Should().NotBeNull();
        result!.RawMessage.Should().BeSameAs(message.Content);
    }

    [Fact]
    public async Task HandleAsync_InheritedBinaryMessage_RawContentProduced()
    {
        InheritedBinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        OutboundEnvelope envelope = new(message, null, TestProducerEndpoint.GetDefault(), Substitute.For<IProducer>());

        IOutboundEnvelope? result = null;
        await new BinaryMessageHandlerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                Substitute.For<IProducer>(),
                Substitute.For<IServiceProvider>()),
            context =>
            {
                result = context.Envelope;
                return default;
            });

        result.Should().NotBeNull();
        result!.RawMessage.Should().BeSameAs(message.Content);
    }

    [Fact]
    public async Task HandleAsync_NonBinaryMessage_EnvelopeUntouched()
    {
        BinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        TestProducerEndpointConfiguration endpointConfiguration = new("test")
        {
            Serializer = new BinaryMessageSerializer()
        };
        OutboundEnvelope envelope = new(message, null, endpointConfiguration.GetDefaultEndpoint(), Substitute.For<IProducer>());

        IOutboundEnvelope? result = null;
        await new BinaryMessageHandlerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                Substitute.For<IProducer>(),
                Substitute.For<IServiceProvider>()),
            context =>
            {
                result = context.Envelope;
                return default;
            });

        result.Should().NotBeNull();
        result!.Should().BeSameAs(envelope);
    }

    [Fact]
    public async Task HandleAsync_EndpointWithBinaryMessageSerializer_EnvelopeUntouched()
    {
        TestEventOne message = new() { Content = "hey!" };
        OutboundEnvelope envelope = new(message, null, TestProducerEndpoint.GetDefault(), Substitute.For<IProducer>());

        IOutboundEnvelope? result = null;
        await new BinaryMessageHandlerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(envelope, Substitute.For<IProducer>(), Substitute.For<IServiceProvider>()),
            context =>
            {
                result = context.Envelope;
                return default;
            });

        result.Should().NotBeNull();
        result!.Should().BeSameAs(envelope);
    }

    private sealed class InheritedBinaryMessage : BinaryMessage
    {
    }
}
