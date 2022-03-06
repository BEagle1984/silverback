// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.BinaryMessages;

public class BinaryMessageHandlerConsumerBehaviorTests
{
    [Fact]
    public async Task HandleAsync_BinaryMessage_BinaryMessageReturned()
    {
        byte[] rawContent = BytesUtil.GetRandomBytes();
        MessageHeader[] headers =
        {
            new("x-message-type", typeof(BinaryMessage).AssemblyQualifiedName)
        };
        RawInboundEnvelope envelope = new(
            rawContent,
            headers,
            TestConsumerEndpoint.GetDefault(),
            new TestOffset());

        IRawInboundEnvelope? result = null;
        await new BinaryMessageHandlerConsumerBehavior().HandleAsync(
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

        result.Should().BeAssignableTo<IInboundEnvelope<BinaryMessage>>();
        BinaryMessage binaryMessage = result.As<IInboundEnvelope<BinaryMessage>>().Message!;
        binaryMessage.Content.ReadAll().Should().BeEquivalentTo(rawContent);
    }

    [Fact]
    public async Task HandleAsync_NoBinaryMessageHeaders_EnvelopeUntouched()
    {
        byte[] rawContent = BytesUtil.GetRandomBytes();
        RawInboundEnvelope envelope = new(
            rawContent,
            null,
            TestConsumerEndpoint.GetDefault(),
            new TestOffset());

        IRawInboundEnvelope? result = null;
        await new BinaryMessageHandlerConsumerBehavior().HandleAsync(
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
    public async Task HandleAsync_EndpointWithBinaryMessageSerializer_EnvelopeUntouched()
    {
        byte[] rawContent = BytesUtil.GetRandomBytes();
        MessageHeader[] headers =
        {
            new("x-message-type", typeof(BinaryMessage).AssemblyQualifiedName)
        };
        TestConsumerConfiguration endpointConfiguration = new("test")
        {
            Serializer = new BinaryMessageSerializer<BinaryMessage>()
        };

        RawInboundEnvelope envelope = new(
            rawContent,
            headers,
            endpointConfiguration.GetDefaultEndpoint(),
            new TestOffset());

        IRawInboundEnvelope? result = null;
        await new BinaryMessageHandlerConsumerBehavior().HandleAsync(
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
