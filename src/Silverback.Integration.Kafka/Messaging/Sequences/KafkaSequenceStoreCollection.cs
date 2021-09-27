// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     This <see cref="ISequenceStoreCollection" /> will create a sequence store per each partition.
    /// </summary>
    internal class KafkaSequenceStoreCollection : ISequenceStoreCollection
    {
        private static readonly TopicPartition AnyTopicPartition = new(string.Empty, Partition.Any);

        private readonly IServiceProvider _serviceProvider;

        private readonly bool _processPartitionsIndependently;

        private readonly ConcurrentDictionary<TopicPartition, ISequenceStore> _sequenceStores = new();

        private bool _disposed;

        public KafkaSequenceStoreCollection(
            IServiceProvider serviceProvider,
            bool processPartitionsIndependently)
        {
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
            _processPartitionsIndependently = processPartitionsIndependently;
        }

        public int Count => _sequenceStores.Count;

        public IEnumerator<ISequenceStore> GetEnumerator() => _sequenceStores.Values.GetEnumerator();

        public ISequenceStore GetSequenceStore(IBrokerMessageIdentifier brokerMessageIdentifier)
        {
            if (brokerMessageIdentifier is not KafkaOffset offset)
                throw new InvalidOperationException("The identifier is not a KafkaOffset.");

            return GetSequenceStore(offset.AsTopicPartition());
        }

        public ISequenceStore GetSequenceStore(TopicPartition topicPartition)
        {
            if (!_processPartitionsIndependently)
                topicPartition = AnyTopicPartition;

            // Try get value before calling GetOrAdd to avoid the closure allocation at every call
            if (_sequenceStores.TryGetValue(topicPartition, out var sequenceStore))
                return sequenceStore;

            return _sequenceStores.GetOrAdd(
                topicPartition,
                _ => _serviceProvider.GetRequiredService<ISequenceStore>());
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            await _sequenceStores.Values.ForEachAsync(store => store.DisposeAsync().AsTask()).ConfigureAwait(false);
            _disposed = true;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
