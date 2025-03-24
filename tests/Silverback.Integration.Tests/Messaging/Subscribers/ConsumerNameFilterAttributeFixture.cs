// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
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
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Subscribers;

public class ConsumerNameFilterAttributeFixture
{
    [Theory]
    [InlineData("consumer1", true)]
    [InlineData("consumer2", true)]
    [InlineData("consumer3", false)]
    public void MustProcess_ShouldFilterByName(string name, bool expectedResult)
    {
        IConfluentConsumerWrapper confluentConsumerWrapper = Substitute.For<IConfluentConsumerWrapper>();
        confluentConsumerWrapper.Initialized.Returns(new AsyncEvent<BrokerClient>());
        confluentConsumerWrapper.Disconnecting.Returns(new AsyncEvent<BrokerClient>());
        InboundEnvelope envelope = new(
            new MemoryStream(),
            [],
            new KafkaConsumerEndpoint(
                "my-topic",
                1,
                new KafkaConsumerEndpointConfiguration()),
            new KafkaConsumer(
                name,
                confluentConsumerWrapper,
                new KafkaConsumerConfiguration(),
                Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
                Substitute.For<IBrokerClientCallbacksInvoker>(),
                Substitute.For<IKafkaOffsetStoreFactory>(),
                Substitute.For<IServiceProvider>(),
                Substitute.For<IConsumerLogger<KafkaConsumer>>()),
            new KafkaOffset(new TopicPartitionOffset("test", 0, 42)));

        bool result = new ConsumerNameFilterAttribute("consumer1", "consumer2").MustProcess(envelope);

        result.ShouldBe(expectedResult);
    }

    [Fact]
    public void MustProcess_ShouldReturnFalse_WhenMessageIsNotInboundEnvelope()
    {
        bool result = new ConsumerNameFilterAttribute().MustProcess(new TestEventOne());

        result.ShouldBe(false);
    }
}
