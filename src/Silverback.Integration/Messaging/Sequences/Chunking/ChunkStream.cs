// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences.Chunking
{
    /// <summary>
    ///     The <see cref="Stream" /> implementation used to read the chunked messages. This stream is used by the
    ///     <see cref="ChunkSequenceReader" /> and it is asynchronously pushed with the chunks being
    ///     consumed.
    /// </summary>
    public class ChunkStream : Stream
    {
        private readonly IMessageStreamEnumerable<IRawInboundEnvelope> _source;

        private IEnumerator<IRawInboundEnvelope>? _syncEnumerator;

        private IAsyncEnumerator<IRawInboundEnvelope>? _asyncEnumerator;

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private byte[]? _currentChunk;

        private int _position;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChunkStream" /> class.
        /// </summary>
        /// <param name="source">
        ///     The chunks composing this stream.
        /// </param>
        public ChunkStream(IMessageStreamEnumerable<IRawInboundEnvelope> source)
        {
            Check.NotNull(source, nameof(source));

            _source = source;
        }

        /// <inheritdoc cref="Stream.CanRead" />
        public override bool CanRead => true;

        /// <inheritdoc cref="Stream.CanSeek" />
        public override bool CanSeek => false;

        /// <inheritdoc cref="Stream.CanTimeout" />
        public override bool CanTimeout => false;

        /// <inheritdoc cref="Stream.CanWrite" />
        public override bool CanWrite => false;

        /// <inheritdoc cref="Stream.Length" />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc cref="Stream.Position" />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc cref="Stream.Flush" />
        public override void Flush() => throw new NotSupportedException();

        /// <inheritdoc cref="Stream.Read(byte[], int, int)" />
        public override int Read(byte[] buffer, int offset, int count)
        {
            Check.NotNull(buffer, nameof(buffer));

            _syncEnumerator ??= _source.GetEnumerator();

            if (_currentChunk == null || _position == _currentChunk.Length)
            {
                if (_syncEnumerator.MoveNext())
                {
                    _currentChunk = _syncEnumerator.Current!.RawMessage.ReadAll();
                    _position = 0;
                }
                else
                {
                    return 0;
                }
            }

            return FillBuffer(buffer, offset, count);
        }

        /// <inheritdoc cref="Stream.ReadAsync(byte[], int, int, CancellationToken)" />
        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            // If cancellation was requested, bail early with an already completed task.
            // Otherwise, return a task that represents the Begin/End methods.
            cancellationToken.ThrowIfCancellationRequested();

            _asyncEnumerator ??= _source.GetAsyncEnumerator(cancellationToken);

            Check.NotNull(buffer, nameof(buffer));

            if (_currentChunk == null || _position == _currentChunk.Length)
            {
                if (await _asyncEnumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    _currentChunk = await _asyncEnumerator.Current.RawMessage.ReadAllAsync().ConfigureAwait(false);
                    _position = 0;
                }
                else
                {
                    return 0;
                }
            }

            return FillBuffer(buffer, offset, count);
        }

        /// <inheritdoc cref="Stream.Seek" />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="Stream.SetLength" />
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="Stream.Write(byte[], int, int)" />
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="Stream.Close" />
        public override void Close()
        {
            // Read the whole source stream to flush it.
            // while (_source.MoveNext())
            // {
            //     // do nothing, just force consuming
            // }
            // TODO: Reimplement!
        }

        /// <inheritdoc cref="Stream.DisposeAsync" />
        public override async ValueTask DisposeAsync()
        {
            if (_asyncEnumerator != null)
                await _asyncEnumerator.DisposeAsync().ConfigureAwait(false);

            await base.DisposeAsync().ConfigureAwait(false);
        }

        /// <inheritdoc cref="Stream.Dispose" />
        protected override void Dispose(bool disposing)
        {
            _syncEnumerator?.Dispose();

            base.Dispose(disposing);
        }

        private int FillBuffer(byte[] buffer, int offset, int count)
        {
            if (_currentChunk == null)
                throw new InvalidOperationException("_currentChunk is null");

            int numberOfBytesToRead = Math.Min(
                buffer.Length - offset,
                Math.Min(count, _currentChunk.Length - _position));
            Buffer.BlockCopy(_currentChunk, _position, buffer, offset, numberOfBytesToRead);
            _position += numberOfBytesToRead;
            return numberOfBytesToRead;
        }
    }
}
