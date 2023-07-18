// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Channels;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    internal abstract class ConsumerChannelsManager<T> : IDisposable
    {
        private readonly int _backpressureLimit;

        protected ConsumerChannelsManager(int channelsCount, int backpressureLimit, Func<ISequenceStore> sequenceStoreFactory)
        {
            _backpressureLimit = backpressureLimit;
            Check.NotNull(sequenceStoreFactory, nameof(sequenceStoreFactory));

            Channels = Enumerable.Range(0, channelsCount)
                .Select(_ => CreateBoundedChannel())
                .ToArray();
            SequenceStores = Enumerable.Range(0, channelsCount)
                .Select(_ => sequenceStoreFactory.Invoke())
                .ToArray();
        }

        public ISequenceStore[] SequenceStores { get; }

        protected Channel<T>[] Channels { get; }

        /// <inheritdocs cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void ResetChannel(int index)
        {
            Channels[index].Writer.TryComplete();
            Channels[index] = CreateBoundedChannel();
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        ///     resources.
        /// </summary>
        /// <param name="disposing">
        ///     A value indicating whether the method has been called by the <c>Dispose</c> method and not from the
        ///     finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            SequenceStores.ForEach(store => store.Dispose());
        }

        private Channel<T> CreateBoundedChannel()
        {
            return Channel.CreateBounded<T>(_backpressureLimit);
        }
    }
}
