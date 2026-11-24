// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Consuming.KafkaOffsetStore;

public class KafkaOffsetStoreScopeTests
{
    [Fact]
    public async Task StoreOffsetAsync_ShouldForwardSequenceOffsetsToStore()
    {
        IKafkaOffsetStore offsetStore = Substitute.For<IKafkaOffsetStore>();
        ISequence sequence = Substitute.For<ISequence>();
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequence: sequence);
        SilverbackContext silverbackContext = context.ServiceProvider.GetRequiredService<SilverbackContext>();

        sequence.GetCommitIdentifiers().Returns(
        [
            new KafkaOffset("topic1", 2, 3),
            new KafkaOffset("topic2", 3, 4)
        ]);

        KafkaOffsetStoreScope scope = new(offsetStore, context);

        await scope.StoreOffsetsAsync();

        await offsetStore.Received(1).StoreOffsetsAsync(
            Arg.Any<string>(),
            Arg.Is<IEnumerable<KafkaOffset>>(enumerable => enumerable.SequenceEqual(
                new[]
                {
                    new KafkaOffset("topic1", 2, 3),
                    new KafkaOffset("topic2", 3, 4)
                })),
            silverbackContext);
    }

    [Fact]
    public async Task StoreOffsetAsync_ShouldForwardEnvelopeOffsetToStore()
    {
        IKafkaOffsetStore offsetStore = Substitute.For<IKafkaOffsetStore>();
        IRawInboundEnvelope envelope = Substitute.For<IRawInboundEnvelope>();
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope);
        SilverbackContext silverbackContext = context.ServiceProvider.GetRequiredService<SilverbackContext>();

        envelope.BrokerMessageIdentifier.Returns(new KafkaOffset("topic1", 4, 2));

        KafkaOffsetStoreScope scope = new(offsetStore, context);

        await scope.StoreOffsetsAsync();

        await offsetStore.Received(1).StoreOffsetsAsync(
            Arg.Any<string>(),
            Arg.Is<IEnumerable<KafkaOffset>>(enumerable => enumerable.SequenceEqual(
                new[]
                {
                    new KafkaOffset("topic1", 4, 2)
                })),
            silverbackContext);
    }

    [Fact]
    public async Task StoreOffsetAsync_ShouldNotForwardOffsets_WhenAllStoreAlready()
    {
        IKafkaOffsetStore offsetStore = Substitute.For<IKafkaOffsetStore>();
        ISequence sequence = Substitute.For<ISequence>();
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequence: sequence);
        SilverbackContext silverbackContext = context.ServiceProvider.GetRequiredService<SilverbackContext>();

        sequence.GetCommitIdentifiers().Returns(
        [
            new KafkaOffset("topic1", 1, 5),
            new KafkaOffset("topic1", 2, 4),
            new KafkaOffset("topic1", 3, 3)
        ]);

        KafkaOffsetStoreScope scope = new(offsetStore, context);

        await scope.StoreOffsetsAsync();
        offsetStore.ClearReceivedCalls();

        sequence.GetCommitIdentifiers().Returns(
        [
            new KafkaOffset("topic1", 1, 5),
            new KafkaOffset("topic1", 2, 4)
        ]);

        await scope.StoreOffsetsAsync();

        await offsetStore.DidNotReceive().StoreOffsetsAsync(
            Arg.Any<string>(),
            Arg.Any<IEnumerable<KafkaOffset>>(),
            silverbackContext);
    }

    [Fact]
    public async Task StoreOffsetAsync_ShouldForwardOffsets_WhenStoredLowerOffset()
    {
        IKafkaOffsetStore offsetStore = Substitute.For<IKafkaOffsetStore>();
        ISequence sequence = Substitute.For<ISequence>();
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequence: sequence);
        SilverbackContext silverbackContext = context.ServiceProvider.GetRequiredService<SilverbackContext>();

        sequence.GetCommitIdentifiers().Returns(
        [
            new KafkaOffset("topic1", 1, 5),
            new KafkaOffset("topic1", 2, 4),
            new KafkaOffset("topic1", 3, 3)
        ]);

        KafkaOffsetStoreScope scope = new(offsetStore, context);

        await scope.StoreOffsetsAsync();
        offsetStore.ClearReceivedCalls();

        sequence.GetCommitIdentifiers().Returns(
        [
            new KafkaOffset("topic1", 1, 6)
        ]);

        await scope.StoreOffsetsAsync();

        await offsetStore.Received(1).StoreOffsetsAsync(
            Arg.Any<string>(),
            Arg.Is<IEnumerable<KafkaOffset>>(enumerable => enumerable.SequenceEqual(
                new[]
                {
                    new KafkaOffset("topic1", 1, 6)
                })),
            silverbackContext);
    }

    [Fact]
    public async Task StoreOffsetAsync_ShouldForwardOffsets_WhenStoredHigherOffset()
    {
        IKafkaOffsetStore offsetStore = Substitute.For<IKafkaOffsetStore>();
        ISequence sequence = Substitute.For<ISequence>();
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequence: sequence);
        SilverbackContext silverbackContext = context.ServiceProvider.GetRequiredService<SilverbackContext>();

        sequence.GetCommitIdentifiers().Returns(
        [
            new KafkaOffset("topic1", 1, 5),
            new KafkaOffset("topic1", 2, 4),
            new KafkaOffset("topic1", 3, 3)
        ]);

        KafkaOffsetStoreScope scope = new(offsetStore, context);

        await scope.StoreOffsetsAsync();
        offsetStore.ClearReceivedCalls();

        sequence.GetCommitIdentifiers().Returns(
        [
            new KafkaOffset("topic1", 1, 2)
        ]);

        await scope.StoreOffsetsAsync();

        await offsetStore.Received(1).StoreOffsetsAsync(
            Arg.Any<string>(),
            Arg.Is<IEnumerable<KafkaOffset>>(enumerable => enumerable.SequenceEqual(
                new[]
                {
                    new KafkaOffset("topic1", 1, 2)
                })),
            silverbackContext);
    }

    [Theory]
    [InlineData("group1", "group1")]
    [InlineData(null, "")]
    public async Task StoreOffsetAsync_ShouldStoreOffsetsOfAssignedPartitionsWithGroupId_WhenConsumerIsKafkaConsumer(
        string? groupId,
        string expectedGroupId)
    {
        IKafkaOffsetStore offsetStore = Substitute.For<IKafkaOffsetStore>();
        ISequence sequence = Substitute.For<ISequence>();
        await using AssignedKafkaConsumer kafkaConsumer = new(
            new KafkaConsumerConfiguration { GroupId = groupId },
            new TopicPartition("topic1", 1));
        await kafkaConsumer.StartAsync();
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(consumer: kafkaConsumer.Consumer, sequence: sequence);
        SilverbackContext silverbackContext = context.ServiceProvider.GetRequiredService<SilverbackContext>();

        sequence.GetCommitIdentifiers().Returns(
        [
            new KafkaOffset("topic1", 1, 5),
            new KafkaOffset("topic1", 2, 4) // Not assigned
        ]);

        KafkaOffsetStoreScope scope = new(offsetStore, context);

        await scope.StoreOffsetsAsync();

        await offsetStore.Received(1).StoreOffsetsAsync(
            expectedGroupId,
            Arg.Is<IEnumerable<KafkaOffset>>(enumerable => enumerable.SequenceEqual(
                new[]
                {
                    new KafkaOffset("topic1", 1, 5)
                })),
            silverbackContext);
    }

    [Fact]
    public async Task StoreOffsetAsync_ShouldNotStoreOffsets_WhenNoPartitionIsAssigned()
    {
        IKafkaOffsetStore offsetStore = Substitute.For<IKafkaOffsetStore>();
        await using AssignedKafkaConsumer kafkaConsumer = new(new KafkaConsumerConfiguration { GroupId = "group1" });
        await kafkaConsumer.StartAsync();
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            consumer: kafkaConsumer.Consumer,
            identifier: new KafkaOffset("topic1", 1, 5));

        KafkaOffsetStoreScope scope = new(offsetStore, context);

        await scope.StoreOffsetsAsync();

        await offsetStore.DidNotReceive().StoreOffsetsAsync(
            Arg.Any<string>(),
            Arg.Any<IEnumerable<KafkaOffset>>(),
            Arg.Any<ISilverbackContext?>());
    }

    [Fact]
    public async Task StoreSkippedOffsetsAsync_ShouldStoreOffsetsOutsideTransaction()
    {
        IKafkaOffsetStore offsetStore = Substitute.For<IKafkaOffsetStore>();
        ISequence sequence = Substitute.For<ISequence>();
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequence: sequence);

        sequence.GetCommitIdentifiers().Returns(
        [
            new KafkaOffset("topic1", 2, 3),
            new KafkaOffset("topic2", 3, 4)
        ]);

        KafkaOffsetStoreScope scope = new(offsetStore, context);

        await scope.StoreSkippedOffsetsAsync();

        await offsetStore.Received(1).StoreOffsetsAsync(
            "",
            Arg.Is<IEnumerable<KafkaOffset>>(enumerable => enumerable.SequenceEqual(
                new[]
                {
                    new KafkaOffset("topic1", 2, 3),
                    new KafkaOffset("topic2", 3, 4)
                })),
            Arg.Is<ISilverbackContext?>(storeContext => storeContext == null));
    }

    [Fact]
    public async Task StoreSkippedOffsetsAsync_ShouldStoreOffsetsAgain_WhenStoredAlready()
    {
        IKafkaOffsetStore offsetStore = Substitute.For<IKafkaOffsetStore>();
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(identifier: new KafkaOffset("topic1", 2, 3));
        SilverbackContext silverbackContext = context.ServiceProvider.GetRequiredService<SilverbackContext>();

        KafkaOffsetStoreScope scope = new(offsetStore, context);

        await scope.StoreOffsetsAsync();
        await scope.StoreSkippedOffsetsAsync();

        Received.InOrder(() =>
        {
            _ = offsetStore.StoreOffsetsAsync(
                Arg.Any<string>(),
                Arg.Is<IEnumerable<KafkaOffset>>(enumerable => enumerable.SequenceEqual(new[] { new KafkaOffset("topic1", 2, 3) })),
                silverbackContext);
            _ = offsetStore.StoreOffsetsAsync(
                Arg.Any<string>(),
                Arg.Is<IEnumerable<KafkaOffset>>(enumerable => enumerable.SequenceEqual(new[] { new KafkaOffset("topic1", 2, 3) })),
                Arg.Is<ISilverbackContext?>(storeContext => storeContext == null));
        });
    }

    [Fact]
    public async Task StoreSkippedOffsetsAsync_ShouldStoreOnlyOffsetsOfAssignedPartitions_WhenConsumerIsKafkaConsumer()
    {
        IKafkaOffsetStore offsetStore = Substitute.For<IKafkaOffsetStore>();
        ISequence sequence = Substitute.For<ISequence>();
        await using AssignedKafkaConsumer kafkaConsumer = new(
            new KafkaConsumerConfiguration { GroupId = "group1" },
            new TopicPartition("topic1", 2));
        await kafkaConsumer.StartAsync();
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(consumer: kafkaConsumer.Consumer, sequence: sequence);

        sequence.GetCommitIdentifiers().Returns(
        [
            new KafkaOffset("topic1", 1, 5), // Not assigned
            new KafkaOffset("topic1", 2, 4)
        ]);

        KafkaOffsetStoreScope scope = new(offsetStore, context);

        await scope.StoreSkippedOffsetsAsync();

        await offsetStore.Received(1).StoreOffsetsAsync(
            "group1",
            Arg.Is<IEnumerable<KafkaOffset>>(enumerable => enumerable.SequenceEqual(new[] { new KafkaOffset("topic1", 2, 4) })),
            Arg.Is<ISilverbackContext?>(storeContext => storeContext == null));
    }

    [Fact]
    public async Task StoreSkippedOffsetsAsync_ShouldNotStoreOffsets_WhenNoPartitionIsAssigned()
    {
        IKafkaOffsetStore offsetStore = Substitute.For<IKafkaOffsetStore>();
        await using AssignedKafkaConsumer kafkaConsumer = new(new KafkaConsumerConfiguration { GroupId = "group1" });
        await kafkaConsumer.StartAsync();
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            consumer: kafkaConsumer.Consumer,
            identifier: new KafkaOffset("topic1", 1, 5));

        KafkaOffsetStoreScope scope = new(offsetStore, context);

        Task storeTask = scope.StoreSkippedOffsetsAsync();

        storeTask.IsCompletedSuccessfully.ShouldBeTrue();
        await offsetStore.DidNotReceive().StoreOffsetsAsync(
            Arg.Any<string>(),
            Arg.Any<IEnumerable<KafkaOffset>>(),
            Arg.Any<ISilverbackContext?>());
    }
}
