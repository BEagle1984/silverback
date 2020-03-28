// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Encryption
{
    public class MessageTransformerFactory : IMessageTransformerFactory
    {
        /// <see cref="IMessageTransformerFactory" />
        public IMessageEncryptor GetEncryptor(EncryptionSettings settings)
        {
            switch (settings)
            {
                case SymmetricEncryptionSettings symmetricEncryptionSettings:
                    return new SymmetricMessageEncryptor(symmetricEncryptionSettings);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <see cref="IMessageTransformerFactory" />
        public IMessageDecryptor GetDecryptor(EncryptionSettings settings)
        {
            switch (settings)
            {
                case SymmetricEncryptionSettings symmetricEncryptionSettings:
                    return new SymmetricMessageDecryptor(symmetricEncryptionSettings);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}