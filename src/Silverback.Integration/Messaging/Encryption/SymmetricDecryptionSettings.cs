// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Encryption
{
    /// <summary>
    ///     The decryption settings used to decrypt the messages.
    /// </summary>
    public record SymmetricDecryptionSettings : SymmetricEncryptionSettingsBase
    {
        /// <summary>
        ///     Gets the function to be used to retrieve the decryption key.
        /// </summary>
        public Func<string?, byte[]>? KeyProvider { get; init; }

        /// <inheritdoc cref="EncryptionSettings.Validate" />
        public override void Validate()
        {
            if (string.IsNullOrEmpty(AlgorithmName))
                throw new EndpointConfigurationException("AlgorithmName cannot be empty.");

            if (KeyProvider == null && Key == null)
                throw new EndpointConfigurationException("Key or DecryptionKey are required.");

            if (KeyProvider != null && Key != null)
                throw new EndpointConfigurationException("Cannot set both Key or DecryptionKey.");
        }
    }
}
