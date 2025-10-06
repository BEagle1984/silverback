// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
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
    public async Task HandleAsync_ShouldReplaceEnvelopeWithBinaryMessage()
    {
        Stream rawContent = BytesUtil.GetRandomStream();
        MessageHeader[] headers =
        [
            new("x-message-type", typeof(BinaryMessage).AssemblyQualifiedName)
        ];
        TestInboundEnvelope<object> envelope = new(
            null,
            rawContent,
            headers,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        IInboundEnvelope? result = null;
        await new BinaryMessageHandlerConsumerBehavior().HandleAsync(
            new ConsumerPipelineContext(
                envelope,
                Substitute.For<IConsumer>(),
                Substitute.For<ISequenceStore>(),
                [],
                Substitute.For<IServiceProvider>()),
            (context, _) =>
            {
                result = context.Envelope;
                return default;
            },
            CancellationToken.None);

        result.ShouldNotBeNull();
        IInboundEnvelope<BinaryMessage> binaryMessageEnvelope = result.ShouldBeAssignableTo<IInboundEnvelope<BinaryMessage>>();
        binaryMessageEnvelope.Message.ShouldNotBeNull();
        binaryMessageEnvelope.Message.Content.ReadAll().ShouldBe(rawContent.ReadAll());
    }

    [Fact]
    public async Task HandleAsync_ShouldNotReplaceEnvelope_WhenNotBinaryMessage()
    {
        Stream rawContent = BytesUtil.GetRandomStream();
        TestInboundEnvelope<object> envelope = new(
            null,
            rawContent,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        IInboundEnvelope? result = null;
        await new BinaryMessageHandlerConsumerBehavior().HandleAsync(
            new ConsumerPipelineContext(
                envelope,
                Substitute.For<IConsumer>(),
                Substitute.For<ISequenceStore>(),
                [],
                Substitute.For<IServiceProvider>()),
            (context, _) =>
            {
                result = context.Envelope;
                return default;
            },
            CancellationToken.None);

        result.ShouldBeSameAs(envelope);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotReplaceEnvelope_WhenEndpointHasBinaryMessageSerializer()
    {
        Stream rawContent = BytesUtil.GetRandomStream();
        MessageHeader[] headers =
        [
            new("x-message-type", typeof(BinaryMessage).AssemblyQualifiedName)
        ];
        TestConsumerEndpointConfiguration endpointConfiguration = new("test")
        {
            Deserializer = new BinaryMessageDeserializer<BinaryMessage>()
        };

        TestInboundEnvelope<object> envelope = new(
            null,
            rawContent,
            headers,
            endpointConfiguration.GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        IInboundEnvelope? result = null;
        await new BinaryMessageHandlerConsumerBehavior().HandleAsync(
            new ConsumerPipelineContext(
                envelope,
                Substitute.For<IConsumer>(),
                Substitute.For<ISequenceStore>(),
                [],
                Substitute.For<IServiceProvider>()),
            (context, _) =>
            {
                result = context.Envelope;
                return default;
            },
            CancellationToken.None);

        result.ShouldBeSameAs(envelope);
    }
}
