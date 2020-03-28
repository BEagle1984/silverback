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
        public SymmetricMessageEncryptor(SymmetricEncryptionSettings settings)
            : base(settings)
        {
        }

        protected override async Task<byte[]> Transform(byte[] message, SymmetricAlgorithm algorithm)
        {
            var encryptedMessage = await base.Transform(message, algorithm);

            if (Settings.InitializationVector != null)
                return encryptedMessage;

            // Prepend the randomly generated IV to the encrypted message
            var fullMessage = new byte[encryptedMessage.Length + algorithm.IV.Length];
            Buffer.BlockCopy(algorithm.IV, 0, fullMessage, 0, algorithm.IV.Length);
            Buffer.BlockCopy(encryptedMessage, 0, fullMessage, algorithm.IV.Length, encryptedMessage.Length);
            return fullMessage;
        }

        protected override ICryptoTransform CreateCryptoTransform(SymmetricAlgorithm algorithm) =>
            algorithm.CreateEncryptor();
    }
}