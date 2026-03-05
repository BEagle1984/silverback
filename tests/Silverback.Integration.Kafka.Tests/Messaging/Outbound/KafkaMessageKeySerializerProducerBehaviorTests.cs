// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
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
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Outbound;

public sealed class KafkaMessageKeySerializerProducerBehaviorTests
{
    [Fact]
    public async Task HandleAsync_ShouldSetRawKey_WhenHeaderSetAndSerializerConfigured()
    {
        string key = "my-key";
        MessageHeaderCollection headers = new()
        {
            { KafkaMessageHeaders.MessageKey, key }
        };
        using KafkaProducer producer = CreateProducer(new Utf8KeySerializer());
        OutboundEnvelope<object> envelope = new(
            new object(),
            headers,
            producer.EndpointConfiguration,
            producer);

        await new KafkaMessageKeySerializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                producer,
                [],
                (_, _) => ValueTask.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        byte[]? expectedRawKey = await GetRawKey(key);
        byte[]? rawKey = await envelope.RawKey.ReadAllAsync();
        rawKey.ShouldBe(expectedRawKey);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotSetRawKey_WhenHeaderSetAndSerializerNotConfigured()
    {
        string key = "my-key";
        MessageHeaderCollection headers = new()
        {
            { KafkaMessageHeaders.MessageKey, key }
        };
        using KafkaProducer producer = CreateProducer(null);
        OutboundEnvelope<object> envelope = new(
            new object(),
            headers,
            producer.EndpointConfiguration,
            producer);

        await new KafkaMessageKeySerializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                producer,
                [],
                (_, _) => ValueTask.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.RawKey.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotSetRawKey_WhenHeaderNotSetAndSerializerNotConfigured()
    {
        using KafkaProducer producer = CreateProducer(null);
        OutboundEnvelope<object> envelope = new(
            new object(),
            [],
            producer.EndpointConfiguration,
            producer);

        await new KafkaMessageKeySerializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(
                envelope,
                producer,
                [],
                (_, _) => ValueTask.CompletedTask,
                Substitute.For<IServiceProvider>()),
            (_, _) => default,
            CancellationToken.None);

        envelope.RawKey.ShouldBeNull();
    }

    private static KafkaProducer CreateProducer(IMessageKeySerializer? keySerializer) =>
        new(
            "producer1",
            Substitute.For<IConfluentProducerWrapper>(),
            new KafkaProducerConfiguration
            {
                Endpoints = new ValueReadOnlyCollection<KafkaProducerEndpointConfiguration>(
                [
                    new KafkaProducerEndpointConfiguration
                    {
                        KeySerializer = keySerializer,
                        EndpointResolver = new KafkaStaticProducerEndpointResolver("topic1")
                    }
                ])
            },
            Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaProducer>>());

    private static async Task<byte[]?> GetRawKey(string key)
    {
        Stream? rawKeyStream = await Utf8KeySerializer.Default.SerializeAsync(key, [], TestProducerEndpoint.GetDefault());
        byte[]? rawKeyBytes = rawKeyStream.ReReadAll();
        return rawKeyBytes;
    }
}
