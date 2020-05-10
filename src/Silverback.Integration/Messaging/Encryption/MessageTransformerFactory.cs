// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Encryption
{
    /// <inheritdoc />
    public class MessageTransformerFactory : IMessageTransformerFactory
    {
        /// <inheritdoc />
        public IMessageEncryptor GetEncryptor(EncryptionSettings settings)
        {
            switch (settings)
            {
                case SymmetricEncryptionSettings symmetricEncryptionSettings:
                    return new SymmetricMessageEncryptor(symmetricEncryptionSettings);
                default:
                    throw new ArgumentOutOfRangeException(nameof(settings));
            }
        }

        /// <inheritdoc />
        public IMessageDecryptor GetDecryptor(EncryptionSettings settings)
        {
            switch (settings)
            {
                case SymmetricEncryptionSettings symmetricEncryptionSettings:
                    return new SymmetricMessageDecryptor(symmetricEncryptionSettings);
                default:
                    throw new ArgumentOutOfRangeException(nameof(settings));
            }
        }
    }
}
