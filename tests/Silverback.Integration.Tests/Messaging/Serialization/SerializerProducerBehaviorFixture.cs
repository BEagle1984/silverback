// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Serialization;

public class SerializerProducerBehaviorFixture
{
    [Fact]
    public async Task HandleAsync_ShouldSetEnvelopeRawMessage()
    {
        IOutboundEnvelope<TestEventOne> envelope = new OutboundEnvelope<TestEventOne>(
            new TestEventOne { Content = "test" },
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        IOutboundEnvelope? result = null;
        await new SerializerProducerBehavior().HandleAsync(
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
        result.RawMessage.ReadAll().ShouldBe("{\"Content\":\"test\"}"u8.ToArray());
    }

    [Fact]
    public async Task HandleAsync_ShouldReplaceTombstoneEnvelope()
    {
        IOutboundEnvelope<ITombstone<TestEventOne>> envelope = new OutboundEnvelope<ITombstone<TestEventOne>>(
            new Tombstone<TestEventOne>("heidi"),
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        IOutboundEnvelope? result = null;
        await new SerializerProducerBehavior().HandleAsync(
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
        result.ShouldBeOfType<OutboundEnvelope<TestEventOne>>();
        result.Message.ShouldBeNull();
        result.RawMessage.ShouldBeNull();
        result.IsTombstone.ShouldBeTrue();
    }
}
