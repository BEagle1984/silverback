// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Diagnostics;

public class KafkaActivityEnricherFixture
{
    [Fact]
    public void EnrichInboundActivity_ShouldAddTags()
    {
        KafkaActivityEnricher enricher = new();

        KafkaOffset offset = new(new TopicPartitionOffset("topic", 3, 42));
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(identifier: offset);
        context.Envelope.Headers[DefaultMessageHeaders.MessageId] = "MessageKey";

        Activity activity = new("Test Activity");

        enricher.EnrichInboundActivity(activity, context);

        activity.Tags.ShouldContain(
            keyValuePair => keyValuePair.Key == KafkaActivityEnricher.KafkaMessageKey &&
                            keyValuePair.Value == "MessageKey");
        activity.Tags.ShouldContain(
            keyValuePair => keyValuePair.Key == KafkaActivityEnricher.KafkaPartition &&
                            keyValuePair.Value == "topic[3]");
        activity.Tags.ShouldContain(
            keyValuePair => keyValuePair.Key == ActivityTagNames.MessageId &&
                            keyValuePair.Value == "topic[3]@42");
    }

    [Fact]
    public void EnrichOutboundActivity_ShouldAddTags()
    {
        KafkaActivityEnricher enricher = new();

        OutboundEnvelope<SingleKeyMemberMessage> envelope = new(
            new SingleKeyMemberMessage(),
            [new MessageHeader(DefaultMessageHeaders.MessageId, "MyKey")],
            new KafkaProducerEndpointConfiguration(),
            Substitute.For<IProducer>());

        ProducerPipelineContext context = new(
            envelope,
            Substitute.For<IProducer>(),
            [],
            (_, _) => ValueTaskFactory.CompletedTask,
            Substitute.For<IServiceProvider>());

        Activity activity = new("Test Activity");
        enricher.EnrichOutboundActivity(activity, context);

        activity.Tags.ShouldContain(keyValuePair => keyValuePair.Key == KafkaActivityEnricher.KafkaMessageKey && keyValuePair.Value == "MyKey");
    }
}
