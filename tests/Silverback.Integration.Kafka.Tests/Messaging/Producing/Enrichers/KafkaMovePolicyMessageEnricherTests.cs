// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Linq;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Enrichers;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Producing.Enrichers;

public sealed class KafkaMovePolicyMessageEnricherTests : IDisposable
{
    private readonly KafkaConsumer _consumer;

    private readonly KafkaConsumerEndpoint _consumerEndpoint;

    private readonly KafkaProducerConfiguration _producerConfiguration;

    public KafkaMovePolicyMessageEnricherTests()
    {
        KafkaConsumerConfiguration consumerConfiguration = new KafkaConsumerConfigurationBuilder(Substitute.For<IServiceProvider>())
            .WithBootstrapServers("PLAINTEXT://tests")
            .WithGroupId("group1")
            .Consume(endpoint => endpoint.ConsumeFrom("source-topic"))
            .Build();

        IConfluentConsumerWrapper confluentConsumerWrapper = Substitute.For<IConfluentConsumerWrapper>();
        confluentConsumerWrapper.Initialized.Returns(new AsyncEvent<BrokerClient>());
        confluentConsumerWrapper.Disconnecting.Returns(new AsyncEvent<BrokerClient>());
        _consumer = new KafkaConsumer(
            "consumer1",
            confluentConsumerWrapper,
            consumerConfiguration,
            Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            Substitute.For<IKafkaOffsetStoreFactory>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaConsumer>>());

        // Subscribed like a topic (any partition), so the source partition can only come from the offset
        _consumerEndpoint = new KafkaConsumerEndpoint("source-topic", Partition.Any, consumerConfiguration.Endpoints.Single());

        _producerConfiguration = new KafkaProducerConfigurationBuilder(Substitute.For<IServiceProvider>())
            .WithBootstrapServers("PLAINTEXT://tests")
            .Produce<object>(endpoint => endpoint.ProduceTo("target-topic"))
            .Build();
    }

    [Fact]
    public void Enrich_ShouldSetSourceHeaders()
    {
        RawInboundEnvelope inboundEnvelope = new(
            Stream.Null,
            [new MessageHeader(KafkaMessageHeaders.Timestamp, "2026-11-23T10:00:00")],
            _consumerEndpoint,
            _consumer,
            new KafkaOffset("source-topic", 3, 42));
        OutboundEnvelope outboundEnvelope = CreateOutboundEnvelope();
        FormatException innermostException = new("innermost")
        {
            Source = "test-source"
        };
        InvalidOperationException exception = new("outer", new ArgumentException("inner", innermostException));

        new KafkaMovePolicyMessageEnricher().Enrich(inboundEnvelope, outboundEnvelope, exception);

        outboundEnvelope.Headers.GetValue(KafkaMessageHeaders.SourceTopic).ShouldBe("source-topic");
        outboundEnvelope.Headers.GetValue(KafkaMessageHeaders.SourceConsumerGroupId).ShouldBe("group1");
        outboundEnvelope.Headers.GetValue(KafkaMessageHeaders.SourcePartition).ShouldBe("3");
        outboundEnvelope.Headers.GetValue(KafkaMessageHeaders.SourceOffset).ShouldBe("42");
        outboundEnvelope.Headers.GetValue(KafkaMessageHeaders.SourceTimestamp).ShouldBe("2026-11-23T10:00:00");
        outboundEnvelope.Headers.GetValue(DefaultMessageHeaders.FailureReason).ShouldBe("System.FormatException in test-source");
    }

    [Fact]
    public void Enrich_ShouldNotSetSourceTimestamp_WhenInboundMessageHasNoTimestamp()
    {
        RawInboundEnvelope inboundEnvelope = new(
            Stream.Null,
            null,
            _consumerEndpoint,
            _consumer,
            new KafkaOffset("source-topic", 3, 42));
        OutboundEnvelope outboundEnvelope = CreateOutboundEnvelope();
        InvalidOperationException exception = new("test")
        {
            Source = "test-source"
        };

        new KafkaMovePolicyMessageEnricher().Enrich(inboundEnvelope, outboundEnvelope, exception);

        outboundEnvelope.Headers.Contains(KafkaMessageHeaders.SourceTimestamp).ShouldBeFalse();
        outboundEnvelope.Headers.GetValue(KafkaMessageHeaders.SourceOffset).ShouldBe("42");
        outboundEnvelope.Headers.GetValue(DefaultMessageHeaders.FailureReason).ShouldBe("System.InvalidOperationException in test-source");
    }

    [Fact]
    public void Enrich_ShouldReplaceExistingHeaders()
    {
        RawInboundEnvelope inboundEnvelope = new(
            Stream.Null,
            null,
            _consumerEndpoint,
            _consumer,
            new KafkaOffset("source-topic", 3, 42));
        OutboundEnvelope outboundEnvelope = CreateOutboundEnvelope();
        outboundEnvelope.Headers.Add(KafkaMessageHeaders.SourceTopic, "old-topic");
        outboundEnvelope.Headers.Add(KafkaMessageHeaders.SourceOffset, "13");

        new KafkaMovePolicyMessageEnricher().Enrich(inboundEnvelope, outboundEnvelope, new InvalidOperationException());

        outboundEnvelope.Headers.Count(header => header.Name == KafkaMessageHeaders.SourceTopic).ShouldBe(1);
        outboundEnvelope.Headers.GetValue(KafkaMessageHeaders.SourceTopic).ShouldBe("source-topic");
        outboundEnvelope.Headers.Count(header => header.Name == KafkaMessageHeaders.SourceOffset).ShouldBe(1);
        outboundEnvelope.Headers.GetValue(KafkaMessageHeaders.SourceOffset).ShouldBe("42");
    }

    public void Dispose() => _consumer.Dispose();

    private OutboundEnvelope CreateOutboundEnvelope() =>
        new(null, null, _producerConfiguration.Endpoints.Single(), Substitute.For<IProducer>());
}
