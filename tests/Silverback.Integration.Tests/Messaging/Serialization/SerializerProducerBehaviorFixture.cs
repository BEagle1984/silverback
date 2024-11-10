// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
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
        result.RawMessage.ReadAll().Should().BeEquivalentTo("{\"Content\":\"test\"}"u8.ToArray());
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
        result.Should().BeOfType<OutboundEnvelope<TestEventOne>>();
        result.Message.Should().BeNull();
        result.RawMessage.Should().BeNull();
        result.IsTombstone.Should().BeTrue();
    }
}
