// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Implements the <c>Encrypt</c> methods.
/// </content>
public abstract partial class ProducerEndpointConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder>
{
    /// <summary>
    ///     Specifies the settings to be used to encrypt the messages.
    /// </summary>
    /// <param name="encryptionSettings">
    ///     The encryption settings.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Encrypt(IEncryptionSettings encryptionSettings)
    {
        _encryptionSettings = Check.NotNull(encryptionSettings, nameof(encryptionSettings));
        return This;
    }

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
