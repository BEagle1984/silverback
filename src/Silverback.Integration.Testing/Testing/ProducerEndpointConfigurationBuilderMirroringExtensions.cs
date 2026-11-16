// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;

namespace Silverback.Testing;

/// <summary>
///     Configures the producer endpoints created by <see cref="ITestingHelper.GetProducerForEndpoint" /> to mirror a consumer endpoint.
/// </summary>
internal static class ProducerEndpointConfigurationBuilderMirroringExtensions
{
    /// <summary>
    ///     Configures the producer endpoint to serialize and encrypt the messages the way the specified consumer endpoint expects them.
    /// </summary>
    /// <typeparam name="TConfiguration">
    ///     The type of the configuration being built.
    /// </typeparam>
    /// <typeparam name="TEndpoint">
    ///     The type of the endpoint.
    /// </typeparam>
    /// <typeparam name="TBuilder">
    ///     The actual builder type.
    /// </typeparam>
    /// <param name="builder">
    ///     The producer endpoint configuration builder.
    /// </param>
    /// <param name="consumerEndpointConfiguration">
    ///     The configuration of the consumer endpoint to be mirrored.
    /// </param>
    public static void MirrorConsumerEndpoint<TConfiguration, TEndpoint, TBuilder>(
        this ProducerEndpointConfigurationBuilder<object, TConfiguration, TEndpoint, TBuilder> builder,
        ConsumerEndpointConfiguration consumerEndpointConfiguration)
        where TConfiguration : ProducerEndpointConfiguration<TEndpoint>
        where TEndpoint : ProducerEndpoint
        where TBuilder : ProducerEndpointConfigurationBuilder<object, TConfiguration, TEndpoint, TBuilder>
    {
        builder.SerializeUsing(consumerEndpointConfiguration.Deserializer.GetCompatibleSerializer());

        IEncryptionSettings? encryptionSettings = GetEncryptionSettings(consumerEndpointConfiguration);

        if (encryptionSettings != null)
            builder.Encrypt(encryptionSettings);
    }

    private static SymmetricEncryptionSettings? GetEncryptionSettings(ConsumerEndpointConfiguration consumerEndpointConfiguration) =>
        consumerEndpointConfiguration.Encryption switch
        {
            null => null,
            SymmetricDecryptionSettings decryptionSettings => new SymmetricEncryptionSettings
            {
                AlgorithmName = decryptionSettings.AlgorithmName,
                BlockSize = decryptionSettings.BlockSize,
                FeedbackSize = decryptionSettings.FeedbackSize,
                InitializationVector = decryptionSettings.InitializationVector,
                Key = decryptionSettings.Key ?? GetKeyFromProvider(decryptionSettings, consumerEndpointConfiguration),
                CipherMode = decryptionSettings.CipherMode,
                PaddingMode = decryptionSettings.PaddingMode
            },
            _ => throw new NotSupportedException(
                $"Cannot mirror the decryption settings of the consumer endpoint '{consumerEndpointConfiguration.DisplayName}', " +
                $"since {consumerEndpointConfiguration.Encryption.GetType().Name} is not supported. " +
                "Use GetProducer to create a producer that encrypts the messages as expected.")
        };

    // Without a key identifier header, the consumer also calls the key provider with a null key identifier
    private static byte[] GetKeyFromProvider(
        SymmetricDecryptionSettings decryptionSettings,
        ConsumerEndpointConfiguration consumerEndpointConfiguration)
    {
        byte[]? key;

        try
        {
            key = decryptionSettings.KeyProvider?.Invoke(null);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(GetKeyProviderErrorMessage(consumerEndpointConfiguration), ex);
        }

        return key ?? throw new InvalidOperationException(GetKeyProviderErrorMessage(consumerEndpointConfiguration));
    }

    private static string GetKeyProviderErrorMessage(ConsumerEndpointConfiguration consumerEndpointConfiguration) =>
        $"Cannot mirror the decryption settings of the consumer endpoint '{consumerEndpointConfiguration.DisplayName}', " +
        "since its key provider doesn't return a key for a null key identifier. " +
        "Use GetProducer to create a producer that encrypts the messages calling EncryptUsingAes(key, keyIdentifier).";
}
