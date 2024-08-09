// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Consuming.KafkaOffsetStore;

public class KafkaOffsetStoreScopeFixture
{
    [Fact]
    public async Task StoreOffsetsAsync_ShouldStoreMessageOffset()
    {
        TestOffsetStore store = new();

        IRawInboundEnvelope envelope = Substitute.For<IRawInboundEnvelope>();
        envelope.BrokerMessageIdentifier.Returns(new KafkaOffset("topic1", 3, 42));
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope);

        KafkaOffsetStoreScope scope = new(store, context);

        await scope.StoreOffsetsAsync();

        IReadOnlyCollection<KafkaOffset> storedOffsets = store.GetStoredOffsets(string.Empty);
        storedOffsets.Should().HaveCount(1);
        storedOffsets.First().Should().BeEquivalentTo(new KafkaOffset("topic1", 3, 42));
    }

    [Fact]
    public async Task StoreOffsetsAsync_ShouldStoreSequenceOffsets()
    {
        TestOffsetStore store = new();

        ISequence sequence = Substitute.For<ISequence>();
        sequence.GetCommitIdentifiers().Returns(
            [
                new KafkaOffset("topic1", 3, 42),
                new KafkaOffset("topic2", 6, 13)
            ]);
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequence: sequence);

        KafkaOffsetStoreScope scope = new(store, context);

        await scope.StoreOffsetsAsync();

        IReadOnlyCollection<KafkaOffset> storedOffsets = store.GetStoredOffsets(string.Empty);
        storedOffsets.Should().HaveCount(2);
        storedOffsets.Should().BeEquivalentTo(
            [
                new KafkaOffset("topic1", 3, 42),
                new KafkaOffset("topic2", 6, 13)
            ],
            options => options.WithoutStrictOrdering());
    }

    private class TestOffsetStore : IKafkaOffsetStore
    {
        private readonly Dictionary<string, IReadOnlyCollection<KafkaOffset>> _offsets = [];

        public IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId) => _offsets[groupId];

        public Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, ISilverbackContext? context = null)
        {
            _offsets[groupId] = offsets.ToArray();
            return Task.CompletedTask;
        }
    }
}
