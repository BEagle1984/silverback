// Copyright (c) 2024 Sergio Aquilini
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
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Outbound;

public sealed class KafkaMessageKeyInitializerProducerBehaviorFixture : IDisposable
{
    private readonly KafkaProducer _kafkaProducer;

    public KafkaMessageKeyInitializerProducerBehaviorFixture()
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
            Substitute.For<IProducerLogger<KafkaProducer>>());
    }

    [Fact]
    public async Task HandleAsync_ShouldLeaveKafkaKeyNull_WhenNoKeyMemberAttribute()
    {
        OutboundEnvelope<NoKeyMembersMessage> envelope = new(
            new NoKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            },
            null,
            new KafkaProducerEndpointConfiguration(),
            _kafkaProducer);

        await new KafkaMessageKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                _kafkaProducer,
                [],
                (_, _) => ValueTaskFactory.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.Headers.GetValue(DefaultMessageHeaders.MessageId).ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_ShouldSetKafkaKeyHeaderFromSingleKeyMemberAttribute()
    {
        OutboundEnvelope<SingleKeyMemberMessage> envelope = new(
            new SingleKeyMemberMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            },
            null,
            new KafkaProducerEndpointConfiguration(),
            _kafkaProducer);

        await new KafkaMessageKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                _kafkaProducer,
                [],
                (_, _) => ValueTaskFactory.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.MessageId, "1"));
    }

    [Fact]
    public async Task HandleAsync_ShouldSetKafkaKeyHeaderFromMultipleKeyMemberAttributes()
    {
        OutboundEnvelope<MultipleKeyMembersMessage> envelope = new(
            new MultipleKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            },
            null,
            new KafkaProducerEndpointConfiguration(),
            _kafkaProducer);

        await new KafkaMessageKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                _kafkaProducer,
                [],
                (_, _) => ValueTaskFactory.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.MessageId, "One=1,Two=2"));
    }

    [Fact]
    public async Task HandleAsync_ShouldNotOverwriteExistingKafkaKeyHeader()
    {
        OutboundEnvelope<SingleKeyMemberMessage> envelope = new(
            new SingleKeyMemberMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            },
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "Heidi!" }
            },
            new KafkaProducerEndpointConfiguration(),
            _kafkaProducer);

        await new KafkaMessageKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                _kafkaProducer,
                [],
                (_, _) => ValueTaskFactory.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.MessageId, "Heidi!"));
    }

    [Fact]
    public async Task HandleAsync_ShouldGenerateKafkaKey_WhenChunkingIsEnabled()
    {
        OutboundEnvelope<NoKeyMembersMessage> envelope = new(
            new NoKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            },
            null,
            new KafkaProducerEndpointConfiguration
            {
                Chunk = new ChunkSettings { Size = 42 }
            },
            _kafkaProducer);

        await new KafkaMessageKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                _kafkaProducer,
                [],
                (_, _) => ValueTaskFactory.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.Headers.GetValue(DefaultMessageHeaders.MessageId).ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldDoNothing_WhenNotKafkaProducer()
    {
        OutboundEnvelope<NoKeyMembersMessage> envelope = new(
            new NoKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            },
            null,
            new KafkaProducerEndpointConfiguration
            {
                Chunk = new ChunkSettings { Size = 42 }
            },
            _kafkaProducer);

        await new KafkaMessageKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                Substitute.For<IProducer>(),
                [],
                (_, _) => ValueTaskFactory.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.Headers.ShouldNotContain(header => header.Name == DefaultMessageHeaders.MessageId);
    }

    public void Dispose() => _kafkaProducer.Dispose();
}
