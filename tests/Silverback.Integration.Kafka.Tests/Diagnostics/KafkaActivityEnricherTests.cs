// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Diagnostics;

public class KafkaActivityEnricherTests
{
    [Fact]
    public void EnrichInboundActivity_ShouldAddTags()
    {
        KafkaActivityEnricher enricher = new();

        KafkaInboundEnvelope<object, string> envelope = new(
            null,
            null,
            new KafkaConsumerEndpoint("topic", 1, new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset(new TopicPartitionOffset("topic", 3, 42)));
        envelope.SetKey("MessageKey");
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope);

        Activity activity = new("Test Activity");

        enricher.EnrichInboundActivity(activity, context);

        activity.Tags.ShouldContain(keyValuePair => keyValuePair.Key == KafkaActivityEnricher.KafkaMessageKey &&
                                                    keyValuePair.Value == "MessageKey");
        activity.Tags.ShouldContain(keyValuePair => keyValuePair.Key == KafkaActivityEnricher.KafkaPartition &&
                                                    keyValuePair.Value == "topic[3]");
        activity.Tags.ShouldContain(keyValuePair => keyValuePair.Key == ActivityTagNames.MessageId &&
                                                    keyValuePair.Value == "topic[3]@42");
    }

    [Fact]
    public void EnrichOutboundActivity_ShouldAddTags()
    {
        KafkaActivityEnricher enricher = new();

        KafkaOutboundEnvelope<SingleKeyMemberMessage, string> envelope = new(
            new SingleKeyMemberMessage(),
            Substitute.For<IProducer>());
        envelope.SetKey("MyKey");

        ProducerPipelineContext context = new(
            envelope,
            Substitute.For<IProducer>(),
            [],
            (_, _) => ValueTask.CompletedTask,
            Substitute.For<IServiceProvider>());

        Activity activity = new("Test Activity");
        enricher.EnrichOutboundActivity(activity, context);

        activity.Tags.ShouldContain(keyValuePair => keyValuePair.Key == KafkaActivityEnricher.KafkaMessageKey && keyValuePair.Value == "MyKey");
    }
}
