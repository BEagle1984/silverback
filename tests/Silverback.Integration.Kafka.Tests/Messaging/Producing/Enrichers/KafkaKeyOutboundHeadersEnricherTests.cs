// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Producing.Enrichers;

public class KafkaKeyOutboundHeadersEnricherTests
{
    private readonly KafkaProducerEndpointConfiguration _endpointConfiguration =
        new KafkaProducerConfigurationBuilder(Substitute.For<IServiceProvider>())
            .WithBootstrapServers("PLAINTEXT://tests")
            .Produce<object>(endpoint => endpoint.ProduceTo("topic1"))
            .Build()
            .Endpoints.Single();

    [Fact]
    public void Enrich_ShouldSetMessageKeyHeaderFromMessage()
    {
        OutboundEnvelope<TestMessage> envelope = CreateEnvelope(new TestMessage { Content = "key1" });
        KafkaKeyOutboundHeadersEnricher<TestMessage> enricher = new(message => message?.Content);

        enricher.Enrich(envelope);

        envelope.Headers.GetValue(KafkaMessageHeaders.MessageKey).ShouldBe("key1");
    }

    [Fact]
    public void Enrich_ShouldSetMessageKeyHeaderFromEnvelope()
    {
        OutboundEnvelope<TestMessage> envelope = CreateEnvelope(new TestMessage { Content = "key1" });
        envelope.Headers.Add("source", "header-value");
        KafkaKeyOutboundHeadersEnricher<TestMessage> enricher = new(
            outboundEnvelope => $"{outboundEnvelope.Message?.Content}-{outboundEnvelope.Headers.GetValue("source")}");

        enricher.Enrich(envelope);

        envelope.Headers.GetValue(KafkaMessageHeaders.MessageKey).ShouldBe("key1-header-value");
    }

    [Fact]
    public void Enrich_ShouldReplaceExistingMessageKeyHeader()
    {
        OutboundEnvelope<TestMessage> envelope = CreateEnvelope(new TestMessage { Content = "key2" });
        envelope.Headers.Add(KafkaMessageHeaders.MessageKey, "key1");
        KafkaKeyOutboundHeadersEnricher<TestMessage> enricher = new(message => message?.Content);

        enricher.Enrich(envelope);

        envelope.Headers.Count(header => header.Name == KafkaMessageHeaders.MessageKey).ShouldBe(1);
        envelope.Headers.GetValue(KafkaMessageHeaders.MessageKey).ShouldBe("key2");
    }

    private OutboundEnvelope<TestMessage> CreateEnvelope(TestMessage message) =>
        new(message, null, _endpointConfiguration, Substitute.For<IProducer>());
}
