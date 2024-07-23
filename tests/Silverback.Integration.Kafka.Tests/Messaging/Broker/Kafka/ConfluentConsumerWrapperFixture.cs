// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using NSubstitute;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

public class ConfluentConsumerWrapperFixture
{
    private readonly IConfluentConsumerBuilder _consumerBuilder = Substitute.For<IConfluentConsumerBuilder>();

    private readonly IConsumer<byte[]?, byte[]?> _confluentConsumer = Substitute.For<IConsumer<byte[]?, byte[]?>>();

    private readonly IBrokerClientCallbacksInvoker _callbacksInvoker = Substitute.For<IBrokerClientCallbacksInvoker>();

    private readonly IConfluentAdminClientFactory _adminClientFactory = Substitute.For<IConfluentAdminClientFactory>();

    private readonly IKafkaOffsetStoreFactory _offsetStoreFactory = Substitute.For<IKafkaOffsetStoreFactory>();

    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    private readonly ISilverbackLogger _logger = Substitute.For<ISilverbackLogger>();

    public ConfluentConsumerWrapperFixture()
    {
        _consumerBuilder.SetConfig(Arg.Any<ConsumerConfig>()).Returns(_consumerBuilder);
        _consumerBuilder.SetErrorHandler(Arg.Any<Action<IConsumer<byte[]?, byte[]?>, Error>>()).Returns(_consumerBuilder);
        _consumerBuilder.Build().Returns(_confluentConsumer);
    }

    [Fact]
    public async Task ConnectAsync_ShouldSubscribeTopics()
    {
        KafkaConsumerConfiguration configuration = new()
        {
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("topic1", Partition.Any, Offset.Beginning),
                        new TopicPartitionOffset("topic2", Partition.Any, Offset.Beginning)
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection()
        };

        ConfluentConsumerWrapper consumer = new(
            "test",
            _consumerBuilder,
            configuration,
            _adminClientFactory,
            _callbacksInvoker,
            _offsetStoreFactory,
            _serviceProvider,
            _logger);

        await consumer.ConnectAsync();

        _confluentConsumer.Received(1).Subscribe(
            Arg.Is<IEnumerable<string>>(
                enumerable =>
                    enumerable.SequenceEqual(new[] { "topic1", "topic2" })));
    }

    [Fact]
    public async Task ConnectAsync_ShouldSubscribeWithRegex()
    {
        KafkaConsumerConfiguration configuration = new()
        {
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("^test_[0-9]*", Partition.Any, Offset.Beginning)
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection()
        };

        ConfluentConsumerWrapper consumer = new(
            "test",
            _consumerBuilder,
            configuration,
            _adminClientFactory,
            _callbacksInvoker,
            _offsetStoreFactory,
            _serviceProvider,
            _logger);

        await consumer.ConnectAsync();

        _confluentConsumer.Received(1).Subscribe(
            Arg.Is<IEnumerable<string>>(
                enumerable =>
                    enumerable.SequenceEqual(new[] { "^test_[0-9]*" })));
    }

    [Fact]
    public async Task ConnectAsync_ShouldAssignSpecificPartitions()
    {
        KafkaConsumerConfiguration configuration = new()
        {
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("topic1", 1, Offset.Beginning),
                        new TopicPartitionOffset("topic1", 2, 42),
                        new TopicPartitionOffset("topic2", 3, Offset.Beginning),
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection()
        };

        ConfluentConsumerWrapper consumer = new(
            "test",
            _consumerBuilder,
            configuration,
            _adminClientFactory,
            _callbacksInvoker,
            _offsetStoreFactory,
            _serviceProvider,
            _logger);

        consumer.Consumer = ConfluentConsumerWrapperFixture.GetKafkaConsumer(consumer);

        await consumer.ConnectAsync();

        _confluentConsumer.Received(1).Assign(
            Arg.Is<IEnumerable<TopicPartitionOffset>>(
                enumerable =>
                    enumerable.SequenceEqual(
                        new[]
                        {
                            new TopicPartitionOffset("topic1", 1, Offset.Beginning),
                            new TopicPartitionOffset("topic1", 2, 42),
                            new TopicPartitionOffset("topic2", 3, Offset.Beginning)
                        })));
    }

    private static KafkaConsumer GetKafkaConsumer(IConfluentConsumerWrapper consumerWrapper) => new(
        "test",
        consumerWrapper,
        new KafkaConsumerConfiguration(),
        Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
        Substitute.For<IBrokerClientCallbacksInvoker>(),
        Substitute.For<IKafkaOffsetStoreFactory>(),
        Substitute.For<IServiceProvider>(),
        Substitute.For<IConsumerLogger<KafkaConsumer>>());
}
