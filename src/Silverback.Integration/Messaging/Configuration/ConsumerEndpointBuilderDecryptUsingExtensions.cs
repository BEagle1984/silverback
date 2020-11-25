// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Encryption;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>DecryptUsingAes</c> method to the
    ///     <see cref="ConsumerEndpointBuilder{TEndpoint,TBuilder}" />.
    /// </summary>
    public static class ConsumerEndpointBuilderDecryptUsingExtensions
    {
        /// <summary>
        ///     Specifies that the AES algorithm has to be used to decrypt the messages.
        /// </summary>
        /// <typeparam name="TBuilder">
        ///     The actual builder type.
        /// </typeparam>
        /// <param name="endpointBuilder">
        ///     The endpoint builder.
        /// </param>
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
        public static TBuilder DecryptUsingAes<TBuilder>(
            this IConsumerEndpointBuilder<TBuilder> endpointBuilder,
            byte[] key,
            byte[]? initializationVector = null)
            where TBuilder : IConsumerEndpointBuilder<TBuilder>
        {
            Check.NotNull(endpointBuilder, nameof(endpointBuilder));

            endpointBuilder.Decrypt(
                new SymmetricEncryptionSettings
                {
                    AlgorithmName = "AES",
                    Key = key,
                    InitializationVector = initializationVector
                });

            return (TBuilder)endpointBuilder;
        }
    }
}
