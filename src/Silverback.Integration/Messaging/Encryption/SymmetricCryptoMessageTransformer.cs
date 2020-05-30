// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Encryption
{
    /// <summary>
    ///     The abstract implementation of either an <see cref="IMessageEncryptor" /> or
    ///     <see cref="IMessageDecryptor" /> based on a <see cref="SymmetricAlgorithm" />.
    /// </summary>
    public abstract class SymmetricCryptoMessageTransformer : IRawMessageTransformer
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SymmetricCryptoMessageTransformer" /> class.
        /// </summary>
        /// <param name="settings">
        ///     The settings such as the algorithm to be used.
        /// </param>
        protected SymmetricCryptoMessageTransformer(SymmetricEncryptionSettings settings)
        {
            Check.NotNull(settings, nameof(settings));

            settings.Validate();

            Settings = settings;
        }

        /// <summary>
        ///     Gets the current encryption settings.
        /// </summary>
        protected SymmetricEncryptionSettings Settings { get; }

        /// <inheritdoc cref="IRawMessageTransformer.TransformAsync" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task<byte[]?> TransformAsync(byte[]? message, MessageHeaderCollection headers)
        {
            if (message == null || message.Length == 0)
                return message;

            using var algorithm = CreateSymmetricAlgorithm();
            return await Transform(message, algorithm);
        }

        /// <summary>
        ///     Applies the encryption.
        /// </summary>
        /// <param name="message">
        ///     The clear text message.
        /// </param>
        /// <param name="algorithm">
        ///     The algorithm to be used.
        /// </param>
        /// <returns>
        ///     The cipher message.
        /// </returns>
        protected virtual async Task<byte[]> Transform(byte[] message, SymmetricAlgorithm algorithm)
        {
            Check.NotNull(message, nameof(message));

            using var cryptoTransform = CreateCryptoTransform(algorithm);
            await using var memoryStream = new MemoryStream();
            await using var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write);

            await cryptoStream.WriteAsync(message, 0, message.Length);
            await cryptoStream.FlushAsync();
            cryptoStream.Close();

            return memoryStream.ToArray();
        }

        /// <summary>
        ///     Creates the <see cref="SymmetricAlgorithm" /> according to the current settings.
        /// </summary>
        /// <returns>
        ///     The <see cref="SymmetricAlgorithm" /> setup with the current settings.
        /// </returns>
        protected virtual SymmetricAlgorithm CreateSymmetricAlgorithm()
        {
            var algorithm = SymmetricAlgorithm.Create(Settings.AlgorithmName);

            if (Settings.BlockSize != null)
                algorithm.BlockSize = Settings.BlockSize.Value;

            if (Settings.FeedbackSize != null)
                algorithm.FeedbackSize = Settings.FeedbackSize.Value;

            if (Settings.BlockSize != null)
                algorithm.BlockSize = Settings.BlockSize.Value;

            if (Settings.InitializationVector != null)
                algorithm.IV = Settings.InitializationVector;

            algorithm.Key = Settings.Key;

            if (Settings.CipherMode != null)
                algorithm.Mode = Settings.CipherMode.Value;

            if (Settings.PaddingMode != null)
                algorithm.Padding = Settings.PaddingMode.Value;

            return algorithm;
        }

        /// <summary>
        ///     Create as new instance of an <see cref="ICryptoTransform" /> to be used to encrypt or decrypt the
        ///     message.
        /// </summary>
        /// <param name="algorithm">
        ///     The <see cref="SymmetricAlgorithm" /> to be used.
        /// </param>
        /// <returns>
        ///     The <see cref="ICryptoTransform" />.
        /// </returns>
        protected abstract ICryptoTransform CreateCryptoTransform(SymmetricAlgorithm algorithm);
    }
}
