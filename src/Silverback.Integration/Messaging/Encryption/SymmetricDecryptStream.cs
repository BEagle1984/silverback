// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using Silverback.Util;

namespace Silverback.Messaging.Encryption
{
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
        ///     The <see cref="SymmetricEncryptionSettings" /> specifying the cryptographic algorithm settings.
        /// </param>
        public SymmetricDecryptStream(Stream stream, SymmetricEncryptionSettings settings)
        {
            Check.NotNull(stream, nameof(stream));
            Check.NotNull(settings, nameof(settings));

            _cryptoTransform = CreateCryptoTransform(settings, stream);
            _cryptoStream = new CryptoStream(stream, _cryptoTransform, CryptoStreamMode.Read);
        }

        /// <inheritdoc cref="SilverbackCryptoStream.CryptoStream" />
        protected override CryptoStream CryptoStream => _cryptoStream;

        /// <inheritdoc cref="IDisposable.Dispose" />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cryptoTransform.Dispose();
                _cryptoStream.Dispose();
            }

            base.Dispose(disposing);
        }

        [SuppressMessage("", "CA2000", Justification = Justifications.NewUsingSyntaxFalsePositive)]
        private static ICryptoTransform CreateCryptoTransform(SymmetricEncryptionSettings settings, Stream stream)
        {
            Check.NotNull(settings, nameof(settings));
            Check.NotNull(stream, nameof(stream));

            using var algorithm = SymmetricAlgorithmFactory.CreateSymmetricAlgorithm(settings);

            if (settings.InitializationVector == null)
            {
                // Read the IV prepended to the message
                byte[] buffer = new byte[algorithm.IV.Length];
                if (stream.Read(buffer, 0, algorithm.IV.Length) == algorithm.IV.Length)
                    algorithm.IV = buffer;
            }

            return algorithm.CreateDecryptor();
        }
    }
}
