// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Security.Cryptography;
using Silverback.Util;

namespace Silverback.Messaging.Encryption;

/// <summary>
///     The implementation of <see cref="SilverbackCryptoStream" /> based on a
///     <see cref="SymmetricAlgorithm" /> used to decrypt the messages.
/// </summary>
public class SymmetricDecryptStream : SilverbackCryptoStream
{
    private readonly ICryptoTransform _cryptoTransform;

    private readonly CryptoStream _cryptoStream;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SymmetricDecryptStream" /> class.
    /// </summary>
    /// <param name="stream">
    ///     The inner <see cref="Stream" /> to read the encrypted message from.
    /// </param>
    /// <param name="settings">
    ///     The <see cref="SymmetricDecryptionSettings" /> specifying the cryptographic algorithm settings.
    /// </param>
    /// <param name="keyIdentifier">
    ///     The key identifier to retrieve the encryption key.
    /// </param>
    public SymmetricDecryptStream(
        Stream stream,
        SymmetricDecryptionSettings settings,
        string? keyIdentifier = null)
    {
        Check.NotNull(stream, nameof(stream));
        Check.NotNull(settings, nameof(settings));

        _cryptoTransform = CreateCryptoTransform(settings, stream, keyIdentifier);
        _cryptoStream = new CryptoStream(stream, _cryptoTransform, CryptoStreamMode.Read);
    }

    /// <inheritdoc cref="SilverbackCryptoStream.CryptoStream" />
    protected override CryptoStream CryptoStream => _cryptoStream;

    /// <inheritdoc cref="Stream.Dispose(bool)" />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cryptoTransform.Dispose();
            _cryptoStream.Dispose();
        }

        base.Dispose(disposing);
    }

    private static ICryptoTransform CreateCryptoTransform(
        SymmetricDecryptionSettings settings,
        Stream stream,
        string? keyIdentifier)
    {
        Check.NotNull(settings, nameof(settings));
        Check.NotNull(stream, nameof(stream));

        byte[] encryptionKey = (settings.KeyProvider != null ? settings.KeyProvider(keyIdentifier) : settings.Key) ??
                               throw new InvalidOperationException("The encryption key is not set.");

        using SymmetricAlgorithm algorithm =
            SymmetricAlgorithmFactory.CreateSymmetricAlgorithm(settings, encryptionKey);

        if (settings.InitializationVector != null)
            return algorithm.CreateDecryptor();

        // Read the IV prepended to the message
        byte[] buffer = new byte[algorithm.IV.Length];

        int totalReadCount = 0;
        while (totalReadCount < algorithm.IV.Length)
        {
            int readCount = stream.Read(buffer, totalReadCount, algorithm.IV.Length - totalReadCount);

            if (readCount == 0)
                break;

            totalReadCount += readCount;
        }

        algorithm.IV = buffer;

        return algorithm.CreateDecryptor();
    }
}
