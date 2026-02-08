// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Outbound;

public sealed class KafkaKeyInitializerProducerBehaviorTests : IDisposable
{
    private readonly KafkaProducer _kafkaProducer;

    public KafkaKeyInitializerProducerBehaviorTests()
    {
        _kafkaProducer = new KafkaProducer(
            "producer1",
            Substitute.For<IConfluentProducerWrapper>(),
            new KafkaProducerConfiguration
            {
                Endpoints = new ValueReadOnlyCollection<KafkaProducerEndpointConfiguration>(
                [
                    new KafkaProducerEndpointConfiguration
                    {
                        EndpointResolver = new KafkaStaticProducerEndpointResolver("topic1")
                    }
                ])
            },
            Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaProducer>>());
    }

    [Fact]
    public async Task HandleAsync_ShouldLeaveKafkaKeyNull_WhenNoKeyMemberAttribute()
    {
        KafkaOutboundEnvelope<NoKeyMembersMessage> envelope = new(
            new NoKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            },
            _kafkaProducer);

        await new KafkaKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                _kafkaProducer,
                [],
                (_, _) => ValueTask.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.Key.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_ShouldSetKafkaKeyHeaderFromSingleKeyMemberAttribute()
    {
        KafkaOutboundEnvelope<SingleKeyMemberMessage> envelope = new(
            new SingleKeyMemberMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            },
            _kafkaProducer);

        await new KafkaKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                _kafkaProducer,
                [],
                (_, _) => ValueTask.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.Key.ShouldBe("1");
    }

    [Fact]
    public async Task HandleAsync_ShouldSetKafkaKeyHeaderFromMultipleKeyMemberAttributes()
    {
        KafkaOutboundEnvelope<MultipleKeyMembersMessage> envelope = new(
            new MultipleKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            },
            _kafkaProducer);

        await new KafkaKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                _kafkaProducer,
                [],
                (_, _) => ValueTask.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.Key.ShouldBe("One=1,Two=2");
    }

    [Fact]
    public async Task HandleAsync_ShouldNotOverwriteExistingKafkaKeyHeader()
    {
        KafkaOutboundEnvelope<SingleKeyMemberMessage> envelope = new(
            new SingleKeyMemberMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            },
            _kafkaProducer);
        envelope.SetKey("Heidi!");

        await new KafkaKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                _kafkaProducer,
                [],
                (_, _) => ValueTask.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.Key.ShouldBe("Heidi!");
    }

    [Fact]
    public async Task HandleAsync_ShouldDoNothing_WhenNotKafkaEnvelope()
    {
        TestOutboundEnvelope<NoKeyMembersMessage> envelope = new(
            new NoKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            },
            _kafkaProducer);

        await new KafkaKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                Substitute.For<IProducer>(),
                [],
                (_, _) => ValueTask.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.Headers.Count.ShouldBe(0);
    }

    public void Dispose() => _kafkaProducer.Dispose();
}
