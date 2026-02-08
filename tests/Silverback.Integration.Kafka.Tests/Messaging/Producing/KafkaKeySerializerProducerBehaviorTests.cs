// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Producing;

public class KafkaKeySerializerProducerBehaviorTests
{
    private readonly IProducer _kafkaProducer;

    public KafkaKeySerializerProducerBehaviorTests()
    {
        _kafkaProducer = Substitute.For<IProducer>();
        _kafkaProducer.EndpointConfiguration.Returns(new KafkaProducerEndpointConfiguration
        {
            KeySerializer = new StringSerializer()
        });
    }

    [Fact]
    public async Task HandleAsync_ShouldSetRawKey()
    {
        KafkaOutboundEnvelope<TestEventOne> envelope = new(new TestEventOne(), _kafkaProducer);
        envelope.SetKafkaKey("some-key");

        await new KafkaKeySerializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                _kafkaProducer,
                [],
                (_, _) => ValueTask.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.RawKey.ShouldBe("some-key"u8.ToArray());
    }

    [Fact]
    public async Task HandleAsync_ShouldNotOverwriteRawKey_WhenNoKeyIsSet()
    {
        KafkaOutboundEnvelope<TestEventOne> envelope = new(new TestEventOne(), _kafkaProducer);
        envelope.SetKafkaRawKey("some-key"u8.ToArray());

        await new KafkaKeySerializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                _kafkaProducer,
                [],
                (_, _) => ValueTask.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.RawKey.ShouldBe("some-key"u8.ToArray());
    }
}
