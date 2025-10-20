// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
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
    public async Task HandleAsync_ShouldSetRawMessage()
    {
        BinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        TestOutboundEnvelope<BinaryMessage> envelope = new(message, new TestProducer());

        IOutboundEnvelope? result = null;
        await new BinaryMessageHandlerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                Substitute.For<IProducer>(),
                [],
                (_, _) => ValueTask.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (context, _) =>
            {
                result = context.Envelope;
                return default;
            },
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.RawMessage.ShouldBeSameAs(message.Content);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetRawMessageFromCustomBinaryMessage()
    {
        InheritedBinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        TestOutboundEnvelope<InheritedBinaryMessage> envelope = new(message, new TestProducer());

        IOutboundEnvelope? result = null;
        await new BinaryMessageHandlerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                Substitute.For<IProducer>(),
                [],
                (_, _) => ValueTask.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (context, _) =>
            {
                result = context.Envelope;
                return default;
            },
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.RawMessage.ShouldBeSameAs(message.Content);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotReplaceEnvelope()
    {
        TestEventOne message = new() { Content = "hey!" };
        TestOutboundEnvelope<TestEventOne> envelope = new(message, Substitute.For<IProducer>());

        IOutboundEnvelope? result = null;
        await new BinaryMessageHandlerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                Substitute.For<IProducer>(),
                [],
                (_, _) => ValueTask.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (context, _) =>
            {
                result = context.Envelope;
                return default;
            },
            CancellationToken.None);

        result.ShouldNotBeNull();
        result!.ShouldBeSameAs(envelope);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotReplaceEnvelope_WhenEndpointHasBinaryMessageSerializer()
    {
        BinaryMessage message = new() { Content = BytesUtil.GetRandomStream() };
        TestProducerEndpointConfiguration endpointConfiguration = new("test")
        {
            Serializer = new BinaryMessageSerializer()
        };
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(endpointConfiguration);
        TestOutboundEnvelope<BinaryMessage> envelope = new(message, producer);

        IOutboundEnvelope? result = null;
        await new BinaryMessageHandlerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                producer,
                [],
                (_, _) => ValueTask.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (context, _) =>
            {
                result = context.Envelope;
                return default;
            },
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldBeSameAs(envelope);
    }

    private sealed class InheritedBinaryMessage : BinaryMessage;
}
