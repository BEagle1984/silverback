// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Silverback.Util;

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
        /// <param name="settings">
        ///     The encryption settings.
        /// </param>
        public SymmetricMessageDecryptor(SymmetricEncryptionSettings settings)
            : base(settings)
        {
        }

        /// <inheritdoc cref="SymmetricCryptoMessageTransformer.TransformAsync(System.IO.Stream,System.Security.Cryptography.SymmetricAlgorithm)" />
        protected override async Task<Stream> TransformAsync(Stream? message, SymmetricAlgorithm algorithm)
        {
            Check.NotNull(message, nameof(message));

            // TODO: Support streaming

            if (Settings.InitializationVector == null)
            {
                await message.ReadAsync(algorithm.IV.AsMemory(0, algorithm.IV.Length)).ConfigureAwait(false);
            }

            return await base.TransformAsync(message, algorithm).ConfigureAwait(false);
        }

        /// <inheritdoc cref="SymmetricCryptoMessageTransformer.CreateCryptoTransform" />
        protected override ICryptoTransform CreateCryptoTransform(SymmetricAlgorithm algorithm) =>
            algorithm.CreateDecryptor();
    }
}
