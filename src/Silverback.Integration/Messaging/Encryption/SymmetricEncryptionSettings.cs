// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Security.Cryptography;

namespace Silverback.Messaging.Encryption
{
    public class SymmetricEncryptionSettings : EncryptionSettings
    {
        /// <summary>
        ///     The name of the specific implementation of the <see cref="SymmetricAlgorithm" /> class to use to
        ///     encrypt or decrypt the messages.
        /// </summary>
        public string AlgorithmName { get; set; } = "AES";

        /// <summary>
        ///     <para>
        ///         The block size, in bits, of the cryptographic operation.
        ///     </para>
        ///     <para>
        ///         If <c>null</c>, the default value for the specified algorithm will be used.
        ///     </para>
        /// </summary>
        public int? BlockSize { get; set; }

        /// <summary>
        ///     <para>
        ///         The feedback size, in bits, of the cryptographic operation for the Cipher Feedback (CFB)
        ///         and Output Feedback (OFB) cipher modes.
        ///     </para>
        ///     <para>
        ///         If <c>null</c>, the default value for the specified algorithm will be used.
        ///     </para>
        /// </summary>
        public int? FeedbackSize { get; set; }

        /// <summary>
        ///     <para>
        ///         The optional initialization vector (IV) for the symmetric algorithm.
        ///     </para>
        ///     <para>
        ///         <b>Important:</b> If <c>null</c> no fixed IV is provided and the producer will automatically
        ///         generate a random one for each message that will also be prepended to the actual encrypted message
        ///         to be available to the consumer.
        ///     </para>
        /// </summary>
        public byte[] InitializationVector { get; set; }

        /// <summary>
        ///     The secret key for the symmetric algorithm.
        /// </summary>
        public byte[] Key { get; set; }

        /// <summary>
        ///     <para>
        ///         The mode for operation of the symmetric algorithm.
        ///     </para>
        ///     <para>
        ///         If <c>null</c>, the default value for the specified algorithm will be used.
        ///     </para>
        /// </summary>
        public CipherMode? CipherMode { get; set; }

        /// <summary>
        ///     <para>
        ///         The padding mode used in the symmetric algorithm.
        ///     </para>
        ///     <para>
        ///         If <c>null</c>, the default value for the specified algorithm will be used.
        ///     </para>
        /// </summary>
        public PaddingMode? PaddingMode { get; set; }

        public override void Validate()
        {
            if (string.IsNullOrEmpty(AlgorithmName))
                throw new EndpointConfigurationException("AlgorithmName cannot be empty.");

            if (Key == null)
                throw new EndpointConfigurationException("Key cannot be null.");
        }
    }
}