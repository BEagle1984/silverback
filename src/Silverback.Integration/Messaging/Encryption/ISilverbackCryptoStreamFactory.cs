// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;

namespace Silverback.Messaging.Encryption
{
    /// <summary>
    ///     The factory used to create the <see cref="SilverbackCryptoStream" /> implementation according to the
    ///     <see cref="EncryptionSettings" />.
    /// </summary>
    public interface ISilverbackCryptoStreamFactory
    {
        /// <summary>
        ///     Gets a <see cref="SilverbackCryptoStream" /> compatible with the specified settings.
        /// </summary>
        /// <param name="stream">
        ///     The inner <see cref="Stream" /> to read the clear-text message from.
        /// </param>
        /// <param name="settings">
        ///     The <see cref="EncryptionSettings" /> specifying the cryptographic algorithm settings.
        /// </param>
        /// <returns>
        ///     A <see cref="SilverbackCryptoStream" /> compatible with the specified settings.
        /// </returns>
        SilverbackCryptoStream GetEncryptStream(Stream stream, EncryptionSettings settings);

        /// <summary>
        ///     Gets a <see cref="SilverbackCryptoStream" /> compatible with the specified settings.
        /// </summary>
        /// <param name="stream">
        ///     The inner <see cref="Stream" /> to read the encrypted message from.
        /// </param>
        /// <param name="settings">
        ///     The <see cref="EncryptionSettings" /> specifying the cryptographic algorithm settings.
        /// </param>
        /// <returns>
        ///     A <see cref="SilverbackCryptoStream" /> compatible with the specified settings.
        /// </returns>
        SilverbackCryptoStream GetDecryptStream(Stream stream, EncryptionSettings settings);
    }
}
