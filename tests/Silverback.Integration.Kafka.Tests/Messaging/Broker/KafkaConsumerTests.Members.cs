// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public partial class KafkaConsumerTests
{
    [Fact]
    public void EndpointsConfiguration_ShouldReturnConfiguredEndpoints()
    {
        IConfluentConsumerWrapper client = CreateSubstituteClient();
        using KafkaConsumer consumer = CreateSubstituteConsumer(client);

        consumer.EndpointsConfiguration.ShouldBeSameAs(consumer.Configuration.Endpoints);
        consumer.EndpointsConfiguration.Single().TopicPartitions.Single().Topic.ShouldBe("topic");
    }

    [Fact]
    public void GetOffsetsForTimestamps_ShouldReturnOffsetsFromClient()
    {
        IConfluentConsumerWrapper client = CreateSubstituteClient();
        TopicPartitionTimestamp[] timestamps = [new(new TopicPartition("topic", 0), new Timestamp(DateTime.UtcNow))];
        TopicPartitionOffset[] offsets = [new(new TopicPartition("topic", 0), 42)];
        client.OffsetsForTimes(timestamps, TimeSpan.FromSeconds(3)).Returns(offsets);
        using KafkaConsumer consumer = CreateSubstituteConsumer(client);

        IReadOnlyList<TopicPartitionOffset> result = consumer.GetOffsetsForTimestamps(timestamps, TimeSpan.FromSeconds(3));

        result.ShouldBeSameAs(offsets);
    }

    [Fact]
    public void StartReadingAssignment_ShouldNotReadAssignment_WhenConsumerIsNotStarted()
    {
        IConfluentConsumerWrapper client = CreateSubstituteClient();
        client.Assignment.Returns([new TopicPartition("topic", 0)]);
        using KafkaConsumer consumer = CreateSubstituteConsumer(client);

        consumer.StartReadingAssignment();

        _ = client.DidNotReceive().Assignment;
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
    }

    [Theory]
    [InlineData("key-1")]
    [InlineData(null)]
    public async Task HandleMessageAsync_ShouldAddKeyAndTimestampHeaders(string? key)
    {
        await using PipelineHarness harness = new(CatchUpMode.None);
        await harness.StartAsync(0);

        await harness.PollAsync(() => PipelineHarness.Record(0, 0, key));
        await PollHarness.WaitUntilAsync(() => harness.Handled.Count == 1);

        IReadOnlyCollection<MessageHeader> headers = harness.Headers.Single();
        headers.ShouldContain(new MessageHeader(KafkaMessageHeaders.Timestamp, "2026-11-23T10:30:00.0000000Z"));

        if (key == null)
            headers.ShouldNotContain(header => header.Name == KafkaMessageHeaders.MessageKey);
        else
            headers.ShouldContain(new MessageHeader(KafkaMessageHeaders.MessageKey, key));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Restart_ShouldNotSeekPartitionNotAssigned_WhenUserSoughtIt(bool independent)
    {
        await using PollHarness harness = new(independent);
        await harness.StartAsync(0);

        // The sought position of a partition that isn't assigned is tracked like any other one, but it can't be sought again
        harness.Consumer.Seek(new TopicPartitionOffset("topic", 1, 10));

        await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);
        await harness.Consumer.StartAsync();
        await harness.PollAsync(() => null);

        // The restart seeks the assigned partition back to its assigned offset, but not the other one
        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 1, 10), new TopicPartitionOffset("topic", 0, 0)]);
        harness.Errors.ShouldBeEmpty();
    }

    private static IConfluentConsumerWrapper CreateSubstituteClient()
    {
        IConfluentConsumerWrapper client = Substitute.For<IConfluentConsumerWrapper>();
        client.Initialized.Returns(new AsyncEvent<BrokerClient>());
        client.Disconnecting.Returns(new AsyncEvent<BrokerClient>());
        client.Status.Returns(ClientStatus.Initialized);

        return client;
    }

    private static KafkaConsumer CreateSubstituteConsumer(IConfluentConsumerWrapper client) =>
        new(
            "consumer",
            client,
            new KafkaConsumerConfiguration
            {
                GroupId = "group",
                Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                [
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            [new TopicPartitionOffset("topic", Partition.Any, Offset.Unset)])
                    }
                ])
            },
            Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            Substitute.For<IKafkaOffsetStoreFactory>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaConsumer>>());
}
