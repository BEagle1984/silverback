// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the Encrypt methods to the <see cref="ProducerConfigurationBuilder{TMessage,TConfiguration,TEndpoint,TBuilder}" />.
/// </content>
public abstract partial class ProducerConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder>
{
    /// <summary>
    ///     Specifies the <see cref="EncryptionSettings" /> to be used to encrypt the messages.
    /// </summary>
    /// <param name="encryptionSettings">
    ///     The <see cref="EncryptionSettings" />.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Encrypt(EncryptionSettings encryptionSettings) =>
        WithEncryption(Check.NotNull(encryptionSettings, nameof(encryptionSettings)));

    /// <summary>
    ///     Specifies that the AES algorithm has to be used to encrypt the messages.
    /// </summary>
    /// <param name="key">
    ///     The secret key for the symmetric algorithm.
    /// </param>
    /// <param name="initializationVector">
    ///     The optional initialization vector (IV) for the symmetric algorithm. If <c>null</c> a different IV
    ///     will be generated for each message and prepended to the actual message payload.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder EncryptUsingAes(byte[] key, byte[]? initializationVector = null) =>
        Encrypt(
            new SymmetricEncryptionSettings
            {
                AlgorithmName = "AES",
                Key = key,
                InitializationVector = initializationVector
            });

    /// <summary>
    ///     Specifies that the AES algorithm has to be used to encrypt the messages.
    /// </summary>
    /// <param name="key">
    ///     The secret key for the symmetric algorithm.
    /// </param>
    /// <param name="keyIdentifier">
    ///     The key identifier to be sent in the header (see <see cref="DefaultMessageHeaders.EncryptionKeyId" />).
    ///     When rotating keys, it will be used on the consumer side to determine the correct key to be used to
    ///     decrypt the message.
    /// </param>
    /// <param name="initializationVector">
    ///     The optional initialization vector (IV) for the symmetric algorithm. If <c>null</c> a different IV
    ///     will be generated for each message and prepended to the actual message payload.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder EncryptUsingAes(byte[] key, string keyIdentifier, byte[]? initializationVector = null) =>
        Encrypt(
            new SymmetricEncryptionSettings
            {
                AlgorithmName = "AES",
                Key = key,
                KeyIdentifier = keyIdentifier,
                InitializationVector = initializationVector
            });
}
