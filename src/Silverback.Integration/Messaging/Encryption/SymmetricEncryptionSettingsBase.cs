// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Silverback.Messaging.Encryption;

/// <summary>
///     The base class for symmetric encryption settings used to encrypt or decrypt the messages being sent through the
///     message broker.
/// </summary>
public abstract record SymmetricEncryptionSettingsBase : EncryptionSettings
{
    /// <summary>
    ///     Gets the name of the specific implementation of the <see cref="SymmetricAlgorithm" /> class
    ///     to use to encrypt or decrypt the messages.
    /// </summary>
    public string AlgorithmName { get; init; } = "AES";

    /// <summary>
    ///     <para>
    ///         Gets the block size, in bits, of the cryptographic operation.
    ///     </para>
    ///     <para>
    ///         If <c>null</c>, the default value for the specified algorithm will be used.
    ///     </para>
    /// </summary>
    public int? BlockSize { get; init; }

    /// <summary>
    ///     <para>
    ///         Gets the feedback size, in bits, of the cryptographic operation for the Cipher Feedback
    ///         (CFB) and Output Feedback (OFB) cipher modes.
    ///     </para>
    ///     <para>
    ///         If <c>null</c>, the default value for the specified algorithm will be used.
    ///     </para>
    /// </summary>
    public int? FeedbackSize { get; init; }

    /// <summary>
    ///     <para>
    ///         Gets the optional initialization vector (IV) for the symmetric algorithm.
    ///     </para>
    ///     <para>
    ///         <b>Important:</b> If <c>null</c> no fixed IV is provided and the producer will automatically
    ///         generate a random one for each message that will also be prepended to the actual encrypted message
    ///         to be available to the
    ///         consumer.
    ///     </para>
    /// </summary>
    [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
    public byte[]? InitializationVector { get; init; }

    /// <summary>
    ///     Gets the secret key for the symmetric algorithm.
    /// </summary>
    [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
    public byte[]? Key { get; init; }

    /// <summary>
    ///     <para>
    ///         Gets the mode for operation of the symmetric algorithm.
    ///     </para>
    ///     <para>
    ///         If <c>null</c>, the default value for the specified algorithm will be used.
    ///     </para>
    /// </summary>
    public CipherMode? CipherMode { get; init; }

    /// <summary>
    ///     <para>
    ///         Gets the padding mode used in the symmetric algorithm.
    ///     </para>
    ///     <para>
    ///         If <c>null</c>, the default value for the specified algorithm will be used.
    ///     </para>
    /// </summary>
    public PaddingMode? PaddingMode { get; init; }

    /// <inheritdoc cref="EncryptionSettings.Validate" />
    public override void Validate()
    {
        if (string.IsNullOrEmpty(AlgorithmName))
            throw new EndpointConfigurationException("The algorithm name is required.", AlgorithmName, nameof(AlgorithmName));

        if (Key == null)
            throw new EndpointConfigurationException("A key is required.", string.Empty, nameof(Key));
    }
}
