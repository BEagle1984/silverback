// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using FluentAssertions;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Diagnostics;

public class KafkaActivityEnricherTests
{
    [Fact]
    public void EnrichInboundActivity_TagsAdded()
    {
        KafkaActivityEnricher enricher = new();

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        context.Envelope.Headers[KafkaMessageHeaders.KafkaMessageKey] = "MessageKey";

        Activity activity = new("Test Activity");

        enricher.EnrichInboundActivity(activity, context);

        IBrokerMessageIdentifier identifier = context.Envelope.BrokerMessageIdentifier;

        activity.Tags.Should().Contain(keyValuePair => keyValuePair.Key == KafkaActivityEnricher.KafkaMessageKey && keyValuePair.Value == "MessageKey");
        activity.Tags.Should()
            .Contain(keyValuePair => keyValuePair.Key == KafkaActivityEnricher.KafkaPartition && keyValuePair.Value == "test");
        activity.Tags.Should()
            .Contain(keyValuePair => keyValuePair.Key == ActivityTagNames.MessageId && keyValuePair.Value == $"{identifier.Key}@{identifier.Value}");
    }

    [Fact]
    public void EnrichOutboundActivity_TagsAdded()
    {
        KafkaActivityEnricher enricher = new();

        OutboundEnvelope<SingleKeyMemberMessage> envelope = new(
            new SingleKeyMemberMessage(),
            new[] { new MessageHeader(KafkaMessageHeaders.KafkaMessageKey, "MyKey") },
            new KafkaProducerEndpoint("test-endpoint", 1, new KafkaProducerConfiguration()));

        ProducerPipelineContext context = new(
            envelope,
            Substitute.For<IProducer>(),
            Substitute.For<IServiceProvider>());

        Activity activity = new("Test Activity");
        enricher.EnrichOutboundActivity(activity, context);

        activity.Tags.Should()
            .Contain(keyValuePair => keyValuePair.Key == KafkaActivityEnricher.KafkaMessageKey && keyValuePair.Value == "MyKey");
    }
}
