// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Silverback.Messaging.Encryption
{
    /// <summary>
    ///     An implementation of <see cref="IMessageEncryptor" /> based on a <see cref="SymmetricAlgorithm" />.
    /// </summary>
    public sealed class SymmetricMessageEncryptor : SymmetricCryptoMessageTransformer, IMessageEncryptor
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SymmetricMessageEncryptor" /> class.
        /// </summary>
        /// <param name="settings">
        ///     The encryption settings.
        /// </param>
        public SymmetricMessageEncryptor(SymmetricEncryptionSettings settings)
            : base(settings)
        {
        }

        /// <inheritdoc cref="SymmetricCryptoMessageTransformer.Transform" />
        protected override async Task<byte[]> Transform(byte[] message, SymmetricAlgorithm algorithm)
        {
            var encryptedMessage = await base.Transform(message, algorithm).ConfigureAwait(false);

            if (Settings.InitializationVector != null)
                return encryptedMessage;

            // Prepend the randomly generated IV to the encrypted message
            var fullMessage = new byte[encryptedMessage.Length + algorithm.IV.Length];
            Buffer.BlockCopy(algorithm.IV, 0, fullMessage, 0, algorithm.IV.Length);
            Buffer.BlockCopy(encryptedMessage, 0, fullMessage, algorithm.IV.Length, encryptedMessage.Length);
            return fullMessage;
        }

        /// <inheritdoc cref="SymmetricCryptoMessageTransformer.CreateCryptoTransform" />
        protected override ICryptoTransform CreateCryptoTransform(SymmetricAlgorithm algorithm) =>
            algorithm.CreateEncryptor();
    }
}
