// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Silverback.Messaging.Encryption
{
    /// <summary>
    ///     An implementation of <see cref="IMessageDecryptor" /> based on a <see cref="SymmetricAlgorithm" />.
    /// </summary>
    public sealed class SymmetricMessageDecryptor : SymmetricCryptoMessageTransformer, IMessageDecryptor
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SymmetricMessageDecryptor" /> class.
        /// </summary>
        /// <param name="settings"> The encryption settings. </param>
        public SymmetricMessageDecryptor(SymmetricEncryptionSettings settings)
            : base(settings)
        {
        }

        /// <inheritdoc />
        protected override Task<byte[]> Transform(byte[] message, SymmetricAlgorithm algorithm)
        {
            if (Settings.InitializationVector == null)
            {
                algorithm.IV = message.Take(algorithm.IV.Length).ToArray();
                message = message.Skip(algorithm.IV.Length).ToArray();
            }

            return base.Transform(message, algorithm);
        }

        /// <inheritdoc />
        protected override ICryptoTransform CreateCryptoTransform(SymmetricAlgorithm algorithm)
        {
            return algorithm.CreateDecryptor();
        }
    }
}
