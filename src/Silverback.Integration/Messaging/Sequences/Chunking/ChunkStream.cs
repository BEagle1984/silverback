// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences.Chunking;

/// <summary>
///     The <see cref="Stream" /> implementation used to read the chunked messages. This stream is used by the
///     <see cref="ChunkSequenceReader" /> and it is asynchronously pushed with the chunks being
///     consumed.
/// </summary>
public sealed class ChunkStream : Stream
{
    private readonly IMessageStreamEnumerable<IRawInboundEnvelope> _source;

    private IEnumerator<IRawInboundEnvelope>? _syncEnumerator;

    private IAsyncEnumerator<IRawInboundEnvelope>? _asyncEnumerator;

    private Memory<byte> _currentChunk;

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
    public override int Read(byte[] buffer, int offset, int count) =>
        Read(Check.NotNull(buffer, nameof(buffer)).AsSpan(offset, count));

    /// <inheritdoc cref="Stream.Read(Span{byte})" />
    public override int Read(Span<byte> buffer)
    {
        if (!ReadCurrentMessage())
            return 0;

        int numberOfBytesToRead = GetNumberOfBytesToRead(buffer.Length);
        _currentChunk.Span.Slice(_position, numberOfBytesToRead).CopyTo(buffer);
        _position += numberOfBytesToRead;
        return numberOfBytesToRead;
    }

    /// <inheritdoc cref="Stream.ReadAsync(byte[], int, int, CancellationToken)" />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        await ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc cref="Stream.ReadAsync(Memory{byte}, CancellationToken)" />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Check.NotNull(buffer, nameof(buffer));

        if (!await ReadCurrentMessageAsync(cancellationToken).ConfigureAwait(false))
            return 0;

        int numberOfBytesToRead = GetNumberOfBytesToRead(buffer.Length);
        _currentChunk.Slice(_position, numberOfBytesToRead).CopyTo(buffer);
        _position += numberOfBytesToRead;
        return numberOfBytesToRead;
    }

    /// <inheritdoc cref="Stream.Seek" />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc cref="Stream.SetLength" />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc cref="Stream.Write(byte[], int, int)" />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc cref="Stream.Close" />
    public override void Close()
    {
        // Nothing to close
    }

    /// <inheritdoc cref="Stream.DisposeAsync" />
    public override async ValueTask DisposeAsync()
    {
        if (_asyncEnumerator != null)
        {
            await _asyncEnumerator.DisposeAsync().ConfigureAwait(false);
            _asyncEnumerator = null;
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc cref="Stream.Dispose(bool)" />
    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Reviewed")]
    protected override void Dispose(bool disposing)
    {
        _asyncEnumerator?.DisposeAsync().AsTask().Wait();
        _asyncEnumerator = null;
        _syncEnumerator?.Dispose();
        _syncEnumerator = null;

        base.Dispose(disposing);
    }

    private bool ReadCurrentMessage()
    {
        _syncEnumerator ??= _source.GetEnumerator();

        if (_currentChunk.Length > 0 && _position < _currentChunk.Length)
            return true;

        while (_syncEnumerator.MoveNext())
        {
            if (_syncEnumerator.Current?.RawMessage == null)
                continue;

            _currentChunk = new Memory<byte>(_syncEnumerator.Current.RawMessage.ReadAll());
            _position = 0;

            return true;
        }

        return false;
    }

    private async Task<bool> ReadCurrentMessageAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _asyncEnumerator ??= _source.GetAsyncEnumerator(cancellationToken);

        if (_currentChunk.Length > 0 && _position < _currentChunk.Length)
            return true;

        while (await _asyncEnumerator.MoveNextAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_asyncEnumerator.Current.RawMessage == null)
                continue;

            _currentChunk = new Memory<byte>(await _asyncEnumerator.Current.RawMessage.ReadAllAsync().ConfigureAwait(false));
            _position = 0;

            return true;
        }

        return false;
    }

    private int GetNumberOfBytesToRead(int count) => Math.Min(count, _currentChunk.Length - _position);
}
