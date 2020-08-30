// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     The <see cref="IEnumerable{T}" /> where the consumed messages are streamed into.
    /// </summary>
    internal sealed class RawInboundEnvelopeEnumerable : IEnumerable<IRawInboundEnvelope>, IDisposable
    {
        private readonly BlockingCollection<EnqueuedEnvelope> _blockingCollection;

        private readonly CancellationTokenSource _completionCancellationTokenSource;

        public RawInboundEnvelopeEnumerable(int collectionSize)
        {
            _blockingCollection = new BlockingCollection<EnqueuedEnvelope>(collectionSize);
            _completionCancellationTokenSource = new CancellationTokenSource();
        }

        public void Add(IRawInboundEnvelope envelope, Action<IRawInboundEnvelope>? processedCallback) =>
            _blockingCollection.Add(new EnqueuedEnvelope(envelope, processedCallback));

        public void Complete()
        {
            _blockingCollection.CompleteAdding();
            _completionCancellationTokenSource.Cancel();
        }

        public IEnumerator<IRawInboundEnvelope> GetEnumerator()
        {
            while (!_blockingCollection.IsCompleted)
            {
                EnqueuedEnvelope? current = null;

                try
                {
                    current = _blockingCollection.Take(_completionCancellationTokenSource.Token);
                }
                catch (OperationCanceledException ex)
                    when (ex.CancellationToken == _completionCancellationTokenSource.Token)
                {
                    // Ignore, it's just the end of the sequence
                }

                if (current != null)
                {
                    yield return current.Envelope;

                    current.ProcessedCallback?.Invoke(current.Envelope);
                }
            }
        }

        public void Dispose()
        {
            Complete();
            _blockingCollection.Dispose();
            _completionCancellationTokenSource.Dispose();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private class EnqueuedEnvelope
        {
            public EnqueuedEnvelope(IRawInboundEnvelope envelope, Action<IRawInboundEnvelope>? processedCallback)
            {
                Envelope = envelope;
                ProcessedCallback = processedCallback;
            }

            public IRawInboundEnvelope Envelope { get; }

            public Action<IRawInboundEnvelope>? ProcessedCallback { get; }
        }
    }
}
