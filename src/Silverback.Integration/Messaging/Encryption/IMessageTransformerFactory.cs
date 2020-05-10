// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Encryption
{
    /// <summary>
    ///     The factory to resolve the <see cref="IMessageEncryptor" /> and <see cref="IMessageDecryptor" />
    ///     according to the <see cref="EncryptionSettings" />.
    /// </summary>
    public interface IMessageTransformerFactory
    {
        /// <summary>
        ///     Gets an <see cref="IMessageEncryptor" /> compatible with the specified settings.
        /// </summary>
        /// <param name="settings">
        ///     The settings such as the algorithm to be used.
        /// </param>
        /// <returns>
        ///     An <see cref="IMessageEncryptor" /> compatible with the specified settings.
        /// </returns>
        IMessageEncryptor GetEncryptor(EncryptionSettings settings);

        /// <summary>
        ///     Gets an <see cref="IMessageDecryptor" /> compatible with the specified settings.
        /// </summary>
        /// <param name="settings">
        ///     The settings such as the algorithm to be used.
        /// </param>
        /// <returns>
        ///     An <see cref="IMessageDecryptor" /> compatible with the specified settings.
        /// </returns>
        IMessageDecryptor GetDecryptor(EncryptionSettings settings);
    }
}
