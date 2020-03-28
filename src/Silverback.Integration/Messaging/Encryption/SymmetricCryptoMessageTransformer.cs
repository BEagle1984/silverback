// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Encryption
{
    /// <summary>
    ///     The abstract implementation of either an <see cref="IMessageEncryptor" /> or <see cref="IMessageDecryptor" />
    ///     based on a <see cref="SymmetricAlgorithm" />.
    /// </summary>
    public abstract class SymmetricCryptoMessageTransformer : IRawMessageTransformer
    {
        protected SymmetricCryptoMessageTransformer(SymmetricEncryptionSettings settings)
        {
            settings.Validate();

            Settings = settings;
        }

        protected SymmetricEncryptionSettings Settings { get; }

        /// <inheritdoc cref="IRawMessageTransformer" />
        public async Task<byte[]> TransformAsync(byte[] message, MessageHeaderCollection headers)
        {
            if (message == null || message.Length == 0)
                return message;

            using var algorithm = CreateSymmetricAlgorithm();
            return await Transform(message, algorithm);
        }

        protected virtual async Task<byte[]> Transform(byte[] message, SymmetricAlgorithm algorithm)
        {
            using var cryptoTransform = CreateCryptoTransform(algorithm);
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write);

            await cryptoStream.WriteAsync(message, 0, message.Length);
            await cryptoStream.FlushAsync();
            cryptoStream.Close();

            return memoryStream.ToArray();
        }

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
        ///     Create as new instance of an <see cref="ICryptoTransform" /> to be used to encrypt or decrypt the message.
        /// </summary>
        /// <returns></returns>
        protected abstract ICryptoTransform CreateCryptoTransform(SymmetricAlgorithm algorithm);
    }
}