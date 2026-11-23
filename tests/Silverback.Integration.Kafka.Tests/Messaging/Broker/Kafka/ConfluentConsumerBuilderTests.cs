// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Shouldly;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Tests.Integration.Kafka.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

// The consumers are built against a broker that is always down or against the librdkafka mock cluster, both on the loopback interface
public class ConfluentConsumerBuilderTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void Build_ShouldThrow_WhenConfigIsNotSet()
    {
        ConfluentConsumerBuilder builder = new();

        Action act = () => builder.Build();

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe("SetConfig must be called to provide the consumer configuration.");
    }

    [Fact]
    public async Task Build_ShouldBuildConsumerWithConfig_WhenNoHandlerIsSet()
    {
        await using DisconnectingLoopbackServer server = new();
        ConfluentConsumerBuilder builder = new();
        builder.SetConfig(GetConfig(server.BootstrapServers));

        using IConsumer<byte[]?, byte[]?> consumer = builder.Build();

        consumer.Name.ShouldStartWith("client1#consumer-");
        consumer.Assignment.ShouldBeEmpty();
        consumer.Consume(TimeSpan.Zero).ShouldBeNull();
    }

    [Fact]
    public async Task Build_ShouldSetStatisticsHandler()
    {
        await using DisconnectingLoopbackServer server = new();
        TaskCompletionSource<(IConsumer<byte[]?, byte[]?> Consumer, string Statistics)> statisticsReceived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConsumerConfig config = GetConfig(server.BootstrapServers);
        config.StatisticsIntervalMs = 100;
        ConfluentConsumerBuilder builder = new();
        builder
            .SetConfig(config)
            .SetStatisticsHandler((consumer, statistics) => statisticsReceived.TrySetResult((consumer, statistics)));

        using IConsumer<byte[]?, byte[]?> consumer = builder.Build();
        PollUntilCompleted(consumer, statisticsReceived.Task);

        (IConsumer<byte[]?, byte[]?> handlerConsumer, string receivedStatistics) = await statisticsReceived.Task.WaitAsync(Timeout);
        handlerConsumer.ShouldBeSameAs(consumer);
        using JsonDocument statisticsDocument = JsonDocument.Parse(receivedStatistics);
        statisticsDocument.RootElement.GetProperty("type").GetString().ShouldBe("consumer");
        statisticsDocument.RootElement.GetProperty("client_id").GetString().ShouldBe("client1");
    }

    [Fact]
    public async Task Build_ShouldSetLogHandler()
    {
        await using DisconnectingLoopbackServer server = new();
        TaskCompletionSource<(IConsumer<byte[]?, byte[]?> Consumer, LogMessage LogMessage)> logReceived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConsumerConfig config = GetConfig(server.BootstrapServers);
        config.Debug = "generic";
        ConfluentConsumerBuilder builder = new();
        builder
            .SetConfig(config)
            .SetLogHandler((consumer, logMessage) => logReceived.TrySetResult((consumer, logMessage)));

        using IConsumer<byte[]?, byte[]?> consumer = builder.Build();
        PollUntilCompleted(consumer, logReceived.Task);

        (IConsumer<byte[]?, byte[]?> handlerConsumer, LogMessage receivedLogMessage) = await logReceived.Task.WaitAsync(Timeout);
        handlerConsumer.ShouldBeSameAs(consumer);
        receivedLogMessage.Level.ShouldBe(SyslogLevel.Debug);
        receivedLogMessage.Name.ShouldBe(consumer.Name);
    }

    [Fact]
    public async Task Build_ShouldSetErrorHandler()
    {
        await using DisconnectingLoopbackServer server = new();
        TaskCompletionSource<(IConsumer<byte[]?, byte[]?> Consumer, Error Error)> allBrokersDownReceived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConfluentConsumerBuilder builder = new();
        builder
            .SetConfig(GetConfig(server.BootstrapServers))
            .SetErrorHandler((consumer, error) =>
            {
                if (error.Code == ErrorCode.Local_AllBrokersDown)
                    allBrokersDownReceived.TrySetResult((consumer, error));
            });

        using IConsumer<byte[]?, byte[]?> consumer = builder.Build();
        PollUntilCompleted(consumer, allBrokersDownReceived.Task);

        (IConsumer<byte[]?, byte[]?> handlerConsumer, Error receivedError) = await allBrokersDownReceived.Task.WaitAsync(Timeout);
        handlerConsumer.ShouldBeSameAs(consumer);
        receivedError.IsFatal.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Build_ShouldSetRebalanceAndOffsetsCommittedHandlers(bool revokedHandlerReturnsOffsets)
    {
        using LibrdkafkaMockCluster mockCluster = new();
        mockCluster.Produce("topic1");
        List<TopicPartition> assignedPartitions = [];
        List<TopicPartitionOffset> revokedPartitions = [];
        List<CommittedOffsets> committedOffsets = [];
        List<string> revokedHandlersInvoked = [];
        ConsumerConfig config = GetConfig(mockCluster.BootstrapServers);

        // The revoked handler can return the offsets only with an eager assignor. The consumer protocol (KIP-848), which is
        // incremental, is used otherwise, since the mock cluster delays the first join of a classic group by a few seconds.
        config.GroupProtocol = revokedHandlerReturnsOffsets ? GroupProtocol.Classic : GroupProtocol.Consumer;
        config.AutoOffsetReset = AutoOffsetReset.Earliest;
        config.AutoCommitIntervalMs = 50;
        ConfluentConsumerBuilder builder = new();
        builder
            .SetConfig(config)
            .SetPartitionsAssignedHandler((_, partitions) => assignedPartitions.AddRange(partitions))
            .SetOffsetsCommittedHandler((_, offsets) => committedOffsets.Add(offsets));

        // The last revoked handler replaces the previous one
        if (revokedHandlerReturnsOffsets)
        {
            builder
                .SetPartitionsRevokedHandler((_, _) => revokedHandlersInvoked.Add("action"))
                .SetPartitionsRevokedHandler((_, partitions) =>
                {
                    revokedHandlersInvoked.Add("func");
                    revokedPartitions.AddRange(partitions);
                    return partitions;
                });
        }
        else
        {
            builder
                .SetPartitionsRevokedHandler((_, partitions) =>
                {
                    revokedHandlersInvoked.Add("func");
                    return partitions;
                })
                .SetPartitionsRevokedHandler((_, partitions) =>
                {
                    revokedHandlersInvoked.Add("action");
                    revokedPartitions.AddRange(partitions);
                });
        }

        using IConsumer<byte[]?, byte[]?> consumer = builder.Build();
        consumer.Subscribe("topic1");

        // The assigned partitions are consumed from the reset offset (earliest), since the handler doesn't return their offsets
        ConsumeResult<byte[]?, byte[]?> result = Consume(consumer);
        PollUntil(consumer, () => committedOffsets.Count > 0);
        consumer.Close();

        result.Offset.ShouldBe(new Offset(0));
        result.Message.Value.ShouldBe([1, 2, 3]);
        assignedPartitions.ShouldNotBeEmpty();
        assignedPartitions.ShouldAllBe(partition => partition.Topic == "topic1");
        assignedPartitions.ShouldContain(result.TopicPartition);
        committedOffsets.ShouldContain(offsets => offsets.Offsets.Any(offset => offset.TopicPartitionOffset == new TopicPartitionOffset(result.TopicPartition, 1)));
        revokedHandlersInvoked.ShouldBe([revokedHandlerReturnsOffsets ? "func" : "action"]);
        revokedPartitions.Select(partition => partition.TopicPartition).ShouldBe(assignedPartitions, ignoreOrder: true);
    }

    [Fact]
    public void Build_ShouldStartFromOffsetsReturnedByPartitionsAssignedHandler()
    {
        using LibrdkafkaMockCluster mockCluster = new();
        TopicPartition topicPartition = new("topic1", 0);
        mockCluster.Produce(topicPartition);
        mockCluster.Produce(topicPartition);
        ConsumerConfig config = GetConfig(mockCluster.BootstrapServers);

        // The consumer protocol keeps the test fast (see above), so no revoked handler returning the offsets can be set
        config.GroupProtocol = GroupProtocol.Consumer;
        config.AutoOffsetReset = AutoOffsetReset.Earliest;
        ConfluentConsumerBuilder builder = new();
        builder
            .SetConfig(config)
            .SetPartitionsAssignedHandler((_, partitions) => partitions.Select(partition => new TopicPartitionOffset(partition, 1)));

        using IConsumer<byte[]?, byte[]?> consumer = builder.Build();
        consumer.Subscribe("topic1");

        ConsumeResult<byte[]?, byte[]?> result = Consume(consumer);
        consumer.Close();

        result.TopicPartitionOffset.ShouldBe(new TopicPartitionOffset(topicPartition, 1));
    }

    private static ConsumerConfig GetConfig(string bootstrapServers) =>
        new()
        {
            BootstrapServers = bootstrapServers,
            ClientId = "client1",
            GroupId = "group1"
        };

    private static void PollUntilCompleted(IConsumer<byte[]?, byte[]?> consumer, Task task) => PollUntil(consumer, () => task.IsCompleted);

    // The consumer runs the callbacks while polling
    private static void PollUntil(IConsumer<byte[]?, byte[]?> consumer, Func<bool> condition)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (!condition() && stopwatch.Elapsed < Timeout)
        {
            consumer.Consume(TimeSpan.FromMilliseconds(10)).ShouldBeNull();
        }
    }

    private static ConsumeResult<byte[]?, byte[]?> Consume(IConsumer<byte[]?, byte[]?> consumer)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        ConsumeResult<byte[]?, byte[]?>? result = null;

        while (result == null && stopwatch.Elapsed < Timeout)
        {
            result = consumer.Consume(TimeSpan.FromMilliseconds(10));
        }

        return result.ShouldNotBeNull();
    }
}
