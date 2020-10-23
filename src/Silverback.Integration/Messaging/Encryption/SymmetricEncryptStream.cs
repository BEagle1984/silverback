// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Encryption
{
    /// <summary>
    ///     The implementation of <see cref="SilverbackCryptoStream" /> based on a
    ///     <see cref="SymmetricAlgorithm" /> used to encrypt the messages.
    /// </summary>
    [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
    public class SymmetricEncryptStream : SilverbackCryptoStream
    {
        private readonly ICryptoTransform _cryptoTransform;

        private readonly CryptoStream _cryptoStream;

        private byte[]? _prefixBuffer;

        private int _prefixBufferPosition;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SymmetricEncryptStream" /> class.
        /// </summary>
        /// <param name="stream">
        ///     The inner <see cref="Stream" /> to read the clear-text message from.
        /// </param>
        /// <param name="settings">
        ///     The <see cref="SymmetricEncryptionSettings" /> specifying the cryptographic algorithm settings.
        /// </param>
        public SymmetricEncryptStream(Stream stream, SymmetricEncryptionSettings settings)
        {
            Check.NotNull(stream, nameof(stream));
            Check.NotNull(settings, nameof(settings));

            _cryptoTransform = CreateCryptoTransform(settings);
            _cryptoStream = new CryptoStream(stream, _cryptoTransform, CryptoStreamMode.Read);
        }

        /// <inheritdoc cref="SilverbackCryptoStream.CryptoStream" />
        protected override CryptoStream CryptoStream => _cryptoStream;

        /// <inheritdoc cref="SilverbackCryptoStream.Read(byte[],int,int)" />
        public override int Read(byte[] buffer, int offset, int count)
        {
            var prefixLength = ReadMessagePrefix(buffer, offset, count);
            if (prefixLength > 0)
                return prefixLength;

            return base.Read(buffer, offset, count);
        }

        /// <inheritdoc cref="SilverbackCryptoStream.ReadAsync(byte[],int,int,CancellationToken)" />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var prefixLength = ReadMessagePrefix(buffer, offset, count);
            if (prefixLength > 0)
                return Task.FromResult(prefixLength);

            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_prefixBuffer != null)
                    Array.Clear(_prefixBuffer, 0, _prefixBuffer.Length);

                _cryptoTransform.Dispose();
                _cryptoStream.Dispose();
            }

            base.Dispose(disposing);
        }

        private int ReadMessagePrefix(byte[] buffer, int offset, int count)
        {
            if (_prefixBuffer == null)
                return 0;

            var length = Math.Min(count, _prefixBuffer.Length - _prefixBufferPosition);
            Array.Copy(_prefixBuffer, _prefixBufferPosition, buffer, offset, length);
            _prefixBufferPosition += length;

            if (_prefixBufferPosition == _prefixBuffer.Length)
            {
                Array.Clear(_prefixBuffer, 0, _prefixBuffer.Length);
                _prefixBuffer = null;
            }

            return length;
        }


        [SuppressMessage("", "CA2000", Justification = Justifications.NewUsingSyntaxFalsePositive)]
        private ICryptoTransform CreateCryptoTransform(SymmetricEncryptionSettings settings)
        {
            using var algorithm = SymmetricAlgorithmFactory.CreateSymmetricAlgorithm(settings);

            if (settings.InitializationVector == null)
                _prefixBuffer = algorithm.IV;

            return algorithm.CreateEncryptor();
        }
    }
}
