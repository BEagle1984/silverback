// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Silverback.Messaging.Encryption
{
    /// <summary>
    ///     The symmetric encryption settings used to encrypt or decrypt the messages being sent through the message broker.
    /// </summary>
    public class SymmetricEncryptionSettings : EncryptionSettings
    {
        /// <summary>
        ///     Gets or sets the name of the specific implementation of the <see cref="SymmetricAlgorithm" /> class to use to
        ///     encrypt or decrypt the messages.
        /// </summary>
        public string AlgorithmName { get; set; } = "AES";

        /// <summary>
        ///     <para>
        ///         Gets or sets the block size, in bits, of the cryptographic operation.
        ///     </para>
        ///     <para>
        ///         If <c>null</c>, the default value for the specified algorithm will be used.
        ///     </para>
        /// </summary>
        public int? BlockSize { get; set; }

        /// <summary>
        ///     <para>
        ///         Gets or sets the feedback size, in bits, of the cryptographic operation for the Cipher Feedback (CFB)
        ///         and Output Feedback (OFB) cipher modes.
        ///     </para>
        ///     <para>
        ///         If <c>null</c>, the default value for the specified algorithm will be used.
        ///     </para>
        /// </summary>
        public int? FeedbackSize { get; set; }

        /// <summary>
        ///     <para>
        ///         Gets or sets the optional initialization vector (IV) for the symmetric algorithm.
        ///     </para>
        ///     <para>
        ///         <b>Important:</b> If <c>null</c> no fixed IV is provided and the producer will automatically
        ///         generate a random one for each message that will also be prepended to the actual encrypted message
        ///         to be available to the consumer.
        ///     </para>
        /// </summary>
        [SuppressMessage("ReSharper", "CA1819", Justification = Justifications.CanExposeByteArray)]
        [SuppressMessage("ReSharper", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public byte[]? InitializationVector { get; set; }

        /// <summary>
        ///     Gets or sets the secret key for the symmetric algorithm.
        /// </summary>
        [SuppressMessage("ReSharper", "CA1819", Justification = Justifications.CanExposeByteArray)]
        [SuppressMessage("ReSharper", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public byte[]? Key { get; set; }

        /// <summary>
        ///     <para>
        ///         Gets or sets the mode for operation of the symmetric algorithm.
        ///     </para>
        ///     <para>
        ///         If <c>null</c>, the default value for the specified algorithm will be used.
        ///     </para>
        /// </summary>
        public CipherMode? CipherMode { get; set; }

        /// <summary>
        ///     <para>
        ///         Gets or sets the padding mode used in the symmetric algorithm.
        ///     </para>
        ///     <para>
        ///         If <c>null</c>, the default value for the specified algorithm will be used.
        ///     </para>
        /// </summary>
        public PaddingMode? PaddingMode { get; set; }

        /// <inheritdoc />
        public override void Validate()
        {
            if (string.IsNullOrEmpty(AlgorithmName))
                throw new EndpointConfigurationException("AlgorithmName cannot be empty.");

            if (Key == null)
                throw new EndpointConfigurationException("Key cannot be null.");
        }
    }
}