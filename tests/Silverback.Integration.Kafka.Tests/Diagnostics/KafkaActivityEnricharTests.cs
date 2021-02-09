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

namespace Silverback.Tests.Integration.Kafka.Diagnostics
{
    public class KafkaActivityEnricharTests
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

            activity.Tags.Should().Contain(
                t => t.Key == KafkaActivityEnricher.KafkaMessageKey && t.Value == "MessageKey");
            activity.Tags.Should()
                .Contain(t => t.Key == KafkaActivityEnricher.KafkaPartition && t.Value == "test");
            activity.Tags.Should().Contain(
                t => t.Key == ActivityTagNames.MessageId &&
                     t.Value == $"{identifier.Key}@{identifier.Value}");
        }

        [Fact]
        public void EnrichOutboundActivity_TagsAdded()
        {
            KafkaActivityEnricher enricher = new();

            var envelope = new OutboundEnvelope<SingleKeyMemberMessage>(
                new SingleKeyMemberMessage(),
                new[] { new MessageHeader(KafkaMessageHeaders.KafkaMessageKey, "MyKey") },
                new KafkaProducerEndpoint("test-endpoint"));

            ProducerPipelineContext context = new(
                envelope,
                Substitute.For<IProducer>(),
                Substitute.For<IServiceProvider>());

            Activity activity = new("Test Activity");
            enricher.EnrichOutboundActivity(activity, context);

            IBrokerMessageIdentifier? identifier = context.Envelope.BrokerMessageIdentifier;

            activity.Tags.Should().Contain(
                t => t.Key == KafkaActivityEnricher.KafkaMessageKey && t.Value == "MyKey");
        }
    }
}
