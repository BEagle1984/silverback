// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Silverback.Util;

namespace Silverback.Messaging.BinaryFiles
{
    /// <summary>
    ///     A read stream backed by an enumerable of byte arrays.
    /// </summary>
    public class ChunkStream : Stream
    {
        private readonly IEnumerator<byte[]> _source;
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private byte[]? _currentChunk;
        private int _position;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChunkStream" /> class.
        /// </summary>
        /// <param name="source">
        ///     The binary chunks composing this stream.
        /// </param>
        public ChunkStream(IEnumerable<byte[]> source)
        {
            Check.NotNull(source, nameof(source));

            _source = source.GetEnumerator();
        }

        /// <inheritdoc cref="Stream.CanRead"/>
        public override bool CanRead => true;

        /// <inheritdoc cref="Stream.CanSeek"/>
        public override bool CanSeek => false;

        /// <inheritdoc cref="Stream.CanTimeout"/>
        public override bool CanTimeout => false;

        /// <inheritdoc cref="Stream.CanWrite"/>
        public override bool CanWrite => false;

        /// <inheritdoc cref="Stream.Length"/>
        public override long Length { get => 0; }

        /// <inheritdoc cref="Stream.Position"/>
        public override long Position
        {
            get => 0; set { }
        }

        /// <inheritdoc cref="Stream.ReadTimeout"/>
        public override int ReadTimeout
        {
            get => 0; set { }
        }

        /// <inheritdoc cref="Stream.WriteTimeout"/>
        public override int WriteTimeout
        {
            get => 0; set { }
        }

        /// <inheritdoc cref="Stream.Flush"/>
        public override void Flush()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="Stream.Read(byte[], int, int)"/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            Check.NotNull(buffer, nameof(buffer));

            if (_currentChunk == null || _position == _currentChunk.Length)
            {
                if (_source.MoveNext())
                {
                    _currentChunk = _source.Current;
                    _position = 0;
                }
                else
                {
                    return 0;
                }
            }

            int numberOfBytesToRead = Math.Min(buffer.Length - offset, Math.Min(count, _currentChunk.Length - _position));
            Buffer.BlockCopy(_currentChunk, _position, buffer, offset, numberOfBytesToRead);
            _position += numberOfBytesToRead;
            return numberOfBytesToRead;
        }

        /// <inheritdoc  cref="Stream.Seek"/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="Stream.SetLength"/>
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc  cref="Stream.Write(byte[], int, int)"/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc  cref="Stream.Close"/>
        public override void Close()
        {
            // Read the whole source stream to flush it.
            while (_source.MoveNext())
            {
                // do nothing, just force consuming
            }

            _source.Dispose();
        }
    }
}
