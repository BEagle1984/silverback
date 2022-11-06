// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using Confluent.Kafka;
using FluentAssertions;
using NSubstitute;
using Silverback.Collections;
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
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Subscribers;

public class KafkaGroupIdFilterAttributeFixture
{
    [Theory]
    [InlineData("group1", true)]
    [InlineData("group2", true)]
    [InlineData("group3", false)]
    public void MustProcess_ShouldFilterByGroupId(string consumerGroupId, bool expectedResult)
    {
        IConfluentConsumerWrapper confluentConsumerWrapper = Substitute.For<IConfluentConsumerWrapper>();
        confluentConsumerWrapper.Initialized.Returns(new AsyncEvent<BrokerClient>());
        confluentConsumerWrapper.Disconnecting.Returns(new AsyncEvent<BrokerClient>());
        InboundEnvelope envelope = new(
            new MemoryStream(),
            new List<MessageHeader>(),
            new KafkaConsumerEndpoint(
                "my-topic",
                1,
                new KafkaConsumerEndpointConfiguration()),
            new KafkaConsumer(
                "42",
                confluentConsumerWrapper,
                new KafkaConsumerConfiguration(
                    new ConsumerConfig
                    {
                        GroupId = consumerGroupId
                    })
                {
                    Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                        new[]
                        {
                            new KafkaConsumerEndpointConfiguration
                            {
                                TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                                    new[]
                                    {
                                        new TopicPartitionOffset("my-topic", 1, Offset.Unset)
                                    })
                            }
                        })
                },
                Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
                Substitute.For<IBrokerClientCallbacksInvoker>(),
                Substitute.For<IKafkaOffsetStoreFactory>(),
                Substitute.For<IServiceProvider>(),
                Substitute.For<IConsumerLogger<KafkaConsumer>>()),
            new KafkaOffset(new TopicPartitionOffset("test", 0, 42)));

        bool result = new KafkaGroupIdFilterAttribute("group1", "group2").MustProcess(envelope);

        result.Should().Be(expectedResult);
    }

    [Fact]
    public void MustProcess_ShouldReturnFalse_WhenMessageIsNotInboundEnvelope()
    {
        bool result = new KafkaGroupIdFilterAttribute().MustProcess(new NoKeyMembersMessage());

        result.Should().BeFalse();
    }

    [Fact]
    public void MustProcess_ShouldReturnFalse_WhenConsumerIsNotKafkaConsumer()
    {
        InboundEnvelope envelope = new(
            new MemoryStream(),
            new List<MessageHeader>(),
            new KafkaConsumerEndpoint(
                "my-topic",
                1,
                new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset(new TopicPartitionOffset("test", 0, 42)));

        bool result = new KafkaGroupIdFilterAttribute().MustProcess(envelope);

        result.Should().BeFalse();
    }
}
