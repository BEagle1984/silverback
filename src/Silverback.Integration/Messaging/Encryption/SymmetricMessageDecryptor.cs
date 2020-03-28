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
        public SymmetricMessageDecryptor(SymmetricEncryptionSettings settings)
            : base(settings)
        {
        }

        protected override Task<byte[]> Transform(byte[] message, SymmetricAlgorithm algorithm)
        {
            if (Settings.InitializationVector == null)
            {
                algorithm.IV = message.Take(algorithm.IV.Length).ToArray();
                message = message.Skip(algorithm.IV.Length).ToArray();
            }

            return base.Transform(message, algorithm);
        }

        protected override ICryptoTransform CreateCryptoTransform(SymmetricAlgorithm algorithm) =>
            algorithm.CreateDecryptor();
    }
}