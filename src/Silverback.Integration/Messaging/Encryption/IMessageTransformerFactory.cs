// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Encryption
{
    /// <summary>
    ///     The factory to resolve the <see cref="IMessageEncryptor" /> and <see cref="IMessageDecryptor" /> according
    ///     to the <see cref="EncryptionSettings" />.
    /// </summary>
    public interface IMessageTransformerFactory
    {
        /// <summary>
        ///     Gets an instance of <see cref="IMessageEncryptor" /> compatible with the specified settings.
        /// </summary>
        IMessageEncryptor GetEncryptor(EncryptionSettings settings);

        /// <summary>
        ///     Gets an instance of <see cref="IMessageDecryptor" /> compatible with the specified settings.
        /// </summary>
        IMessageDecryptor GetDecryptor(EncryptionSettings settings);
    }
}