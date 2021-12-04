// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Encryption;

/// <summary>
///     The base class for the <see cref="Stream" /> implementations used to encrypt and decrypt the
///     integration messages.
/// </summary>
public abstract class SilverbackCryptoStream : Stream
{
    /// <inheritdoc cref="System.Security.Cryptography.CryptoStream.CanRead" />
    public override bool CanRead => CryptoStream.CanRead;

    /// <inheritdoc cref="System.Security.Cryptography.CryptoStream.CanSeek" />
    public override bool CanSeek => CryptoStream.CanSeek;

    /// <inheritdoc cref="System.Security.Cryptography.CryptoStream.CanWrite" />
    public override bool CanWrite => CryptoStream.CanWrite;

    /// <inheritdoc cref="System.Security.Cryptography.CryptoStream.Length" />
    public override long Length => CryptoStream.Length;

    /// <inheritdoc cref="System.Security.Cryptography.CryptoStream.Position" />
    public override long Position
    {
        get => CryptoStream.Position;
        set => CryptoStream.Position = value;
    }

    /// <summary>
    ///     Gets the underlying <see cref="CryptoStream" />.
    /// </summary>
    protected abstract CryptoStream CryptoStream { get; }

    /// <inheritdoc cref="System.Security.Cryptography.CryptoStream.Flush" />
    public override void Flush() => CryptoStream.Flush();

    /// <inheritdoc cref="System.Security.Cryptography.CryptoStream.FlushAsync(CancellationToken)" />
    public override Task FlushAsync(CancellationToken cancellationToken) =>
        CryptoStream.FlushAsync(cancellationToken);

    /// <inheritdoc cref="System.Security.Cryptography.CryptoStream.Read(byte[],int,int)" />
    public override int Read(byte[] buffer, int offset, int count) =>
        CryptoStream.Read(buffer, offset, count);

    /// <inheritdoc cref="System.Security.Cryptography.CryptoStream.ReadAsync(byte[],int,int,CancellationToken)" />
    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken) =>
        CryptoStream.ReadAsync(buffer, offset, count, cancellationToken);

    /// <inheritdoc cref="System.Security.Cryptography.CryptoStream.Seek" />
    public override long Seek(long offset, SeekOrigin origin) => CryptoStream.Seek(offset, origin);

    /// <inheritdoc cref="System.Security.Cryptography.CryptoStream.SetLength" />
    public override void SetLength(long value) => CryptoStream.SetLength(value);

    /// <inheritdoc cref="System.Security.Cryptography.CryptoStream.Write(byte[],int,int)" />
    public override void Write(byte[] buffer, int offset, int count) => CryptoStream.Write(buffer, offset, count);

    /// <inheritdoc cref="System.Security.Cryptography.CryptoStream.WriteAsync(byte[],int,int,CancellationToken)" />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        CryptoStream.WriteAsync(buffer, offset, count, cancellationToken);

    /// <inheritdoc cref="Stream.Close" />
    public override void Close() => CryptoStream.Close();
}
