// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Security.Cryptography;

namespace Silverback.Messaging.Encryption
{
    /// <inheritdoc cref="ISilverbackCryptoStreamFactory" />
    public class SilverbackCryptoStreamFactory : ISilverbackCryptoStreamFactory
    {
        /// <inheritdoc cref="ISilverbackCryptoStreamFactory.GetEncryptStream" />
        public SilverbackCryptoStream GetEncryptStream(Stream stream, EncryptionSettings settings)
        {
            switch (settings)
            {
                case SymmetricEncryptionSettings symmetricEncryptionSettings:
                    return new SymmetricEncryptStream(stream, symmetricEncryptionSettings);
                default:
                    throw new ArgumentOutOfRangeException(nameof(settings), settings, null);
            }
        }

        /// <inheritdoc cref="ISilverbackCryptoStreamFactory.GetDecryptStream" />
        public SilverbackCryptoStream GetDecryptStream(Stream stream, EncryptionSettings settings)
        {
            switch (settings)
            {
                case SymmetricEncryptionSettings symmetricEncryptionSettings:
                    return new SymmetricDecryptStream(stream, symmetricEncryptionSettings);
                default:
                    throw new ArgumentOutOfRangeException(nameof(settings), settings, null);
            }
        }
    }
}
