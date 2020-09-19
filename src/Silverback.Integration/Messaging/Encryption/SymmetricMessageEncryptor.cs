// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
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
        protected override async Task<Stream> Transform(Stream message, SymmetricAlgorithm algorithm)
        {
            // TODO: Support streaming (don't read the entire stream)

            var encryptedMessage = await base.Transform(message, algorithm).ConfigureAwait(false);

            if (Settings.InitializationVector != null)
                return encryptedMessage;

            var memoryStream = new MemoryStream();

            // Prepend the randomly generated IV to the encrypted message
            memoryStream.Write(algorithm.IV);

            await encryptedMessage.CopyToAsync(memoryStream).ConfigureAwait(false);

            return memoryStream;
        }

        /// <inheritdoc cref="SymmetricCryptoMessageTransformer.CreateCryptoTransform" />
        protected override ICryptoTransform CreateCryptoTransform(SymmetricAlgorithm algorithm) =>
            algorithm.CreateEncryptor();
    }
}
