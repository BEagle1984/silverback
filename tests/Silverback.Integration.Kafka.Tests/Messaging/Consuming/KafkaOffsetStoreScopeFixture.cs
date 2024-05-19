// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Consuming;

public class KafkaOffsetStoreScopeFixture
{
    [Fact]
    public async Task StoreOffsetAsync_ShouldForwardSequenceOffsetsToStore()
    {
        IKafkaOffsetStore offsetStore = Substitute.For<IKafkaOffsetStore>();
        ISequence sequence = Substitute.For<ISequence>();
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequence: sequence);
        SilverbackContext silverbackContext = context.ServiceProvider.GetRequiredService<SilverbackContext>();

        sequence.GetCommitIdentifiers().Returns(
            new[]
            {
                new KafkaOffset("topic1", 2, 3),
                new KafkaOffset("topic2", 3, 4)
            });

        KafkaOffsetStoreScope scope = new(offsetStore, context);

        await scope.StoreOffsetsAsync();

        await offsetStore.Received(1).StoreOffsetsAsync(
            Arg.Any<string>(),
            Arg.Is<IEnumerable<KafkaOffset>>(
                enumerable => enumerable.SequenceEqual(
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
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope: envelope);
        SilverbackContext silverbackContext = context.ServiceProvider.GetRequiredService<SilverbackContext>();

        envelope.BrokerMessageIdentifier.Returns(new KafkaOffset("topic1", 4, 2));

        KafkaOffsetStoreScope scope = new(offsetStore, context);

        await scope.StoreOffsetsAsync();

        await offsetStore.Received(1).StoreOffsetsAsync(
            Arg.Any<string>(),
            Arg.Is<IEnumerable<KafkaOffset>>(
                enumerable => enumerable.SequenceEqual(
                    new[]
                    {
                        new KafkaOffset("topic1", 4, 2),
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
            new[]
            {
                new KafkaOffset("topic1", 1, 5),
                new KafkaOffset("topic1", 2, 4),
                new KafkaOffset("topic1", 3, 3),
            });

        KafkaOffsetStoreScope scope = new(offsetStore, context);

        await scope.StoreOffsetsAsync();
        offsetStore.ClearReceivedCalls();

        sequence.GetCommitIdentifiers().Returns(
            new[]
            {
                new KafkaOffset("topic1", 1, 5),
                new KafkaOffset("topic1", 2, 4)
            });

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
            new[]
            {
                new KafkaOffset("topic1", 1, 5),
                new KafkaOffset("topic1", 2, 4),
                new KafkaOffset("topic1", 3, 3),
            });

        KafkaOffsetStoreScope scope = new(offsetStore, context);

        await scope.StoreOffsetsAsync();
        offsetStore.ClearReceivedCalls();

        sequence.GetCommitIdentifiers().Returns(
            new[]
            {
                new KafkaOffset("topic1", 1, 6)
            });

        await scope.StoreOffsetsAsync();

        await offsetStore.Received(1).StoreOffsetsAsync(
            Arg.Any<string>(),
            Arg.Is<IEnumerable<KafkaOffset>>(
                enumerable => enumerable.SequenceEqual(
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
            new[]
            {
                new KafkaOffset("topic1", 1, 5),
                new KafkaOffset("topic1", 2, 4),
                new KafkaOffset("topic1", 3, 3),
            });

        KafkaOffsetStoreScope scope = new(offsetStore, context);

        await scope.StoreOffsetsAsync();
        offsetStore.ClearReceivedCalls();

        sequence.GetCommitIdentifiers().Returns(
            new[]
            {
                new KafkaOffset("topic1", 1, 2)
            });

        await scope.StoreOffsetsAsync();

        await offsetStore.Received(1).StoreOffsetsAsync(
            Arg.Any<string>(),
            Arg.Is<IEnumerable<KafkaOffset>>(
                enumerable => enumerable.SequenceEqual(
                    new[]
                    {
                        new KafkaOffset("topic1", 1, 2)
                    })),
            silverbackContext);
    }
}
