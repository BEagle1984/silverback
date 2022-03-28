// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Implements the <c>Decrypt</c> methods.
/// </content>
public abstract partial class ConsumerEndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder>
{
    /// <summary>
    ///     Specifies the settings to be used to decrypt the messages.
    /// </summary>
    /// <param name="encryptionSettings">
    ///     The <see cref="IDecryptionSettings" />.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Decrypt(IDecryptionSettings encryptionSettings)
    {
        _encryptionSettings = Check.NotNull(encryptionSettings, nameof(encryptionSettings));
        return This;
    }

    /// <summary>
    ///     Specifies that the AES algorithm has to be used to decrypt the messages.
    /// </summary>
    /// <param name="decryptionKeyProvider">
    ///     The function to be used to retrieve the encryption key according to the identifier passed in the header (see
    ///     <see cref="DefaultMessageHeaders.EncryptionKeyId" />).
    /// </param>
    /// <param name="initializationVector">
    ///     The optional initialization vector (IV) for the symmetric algorithm. If <c>null</c> it is expected
    ///     that the IV is prepended to the actual encrypted message.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder DecryptUsingAes(Func<string?, byte[]> decryptionKeyProvider, byte[]? initializationVector = null) =>
        Decrypt(
            new SymmetricDecryptionSettings
            {
                AlgorithmName = "AES",
                KeyProvider = decryptionKeyProvider,
                InitializationVector = initializationVector
            });

    /// <summary>
    ///     Specifies that the AES algorithm has to be used to decrypt the messages.
    /// </summary>
    /// <param name="key">
    ///     The secret key for the symmetric algorithm.
    /// </param>
    /// <param name="initializationVector">
    ///     The optional initialization vector (IV) for the symmetric algorithm. If <c>null</c> it is expected
    ///     that the IV is prepended to the actual encrypted message.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder DecryptUsingAes(byte[] key, byte[]? initializationVector = null) =>
        Decrypt(
            new SymmetricDecryptionSettings
            {
                AlgorithmName = "AES",
                Key = key,
                InitializationVector = initializationVector
            });
}
