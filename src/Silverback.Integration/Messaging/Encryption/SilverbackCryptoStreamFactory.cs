// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;

namespace Silverback.Messaging.Encryption;

/// <inheritdoc cref="ISilverbackCryptoStreamFactory" />
public class SilverbackCryptoStreamFactory : ISilverbackCryptoStreamFactory
{
    /// <inheritdoc cref="ISilverbackCryptoStreamFactory.GetEncryptStream" />
    public SilverbackCryptoStream GetEncryptStream(Stream stream, IEncryptionSettings settings) =>
        settings switch
        {
            SymmetricEncryptionSettings symmetricEncryptionSettings => new SymmetricEncryptStream(
                stream,
                symmetricEncryptionSettings),
            _ => throw new ArgumentOutOfRangeException(nameof(settings), settings, null)
        };

    /// <inheritdoc cref="ISilverbackCryptoStreamFactory.GetDecryptStream" />
    public SilverbackCryptoStream GetDecryptStream(Stream stream, IDecryptionSettings settings, string? keyIdentifier = null) =>
        settings switch
        {
            SymmetricDecryptionSettings symmetricEncryptionSettings => new SymmetricDecryptStream(
                stream,
                symmetricEncryptionSettings,
                keyIdentifier),
            _ => throw new ArgumentOutOfRangeException(nameof(settings), settings, null)
        };
}
