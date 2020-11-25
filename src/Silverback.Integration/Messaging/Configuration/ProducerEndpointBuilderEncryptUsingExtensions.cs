// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Encryption;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>EncryptUsingAes</c> method to the
    ///     <see cref="ProducerEndpointBuilder{TEndpoint,TBuilder}" />.
    /// </summary>
    public static class ProducerEndpointBuilderEncryptUsingExtensions
    {
        /// <summary>
        ///     Specifies that the AES algorithm has to be used to encrypt the messages.
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
        ///     The optional initialization vector (IV) for the symmetric algorithm. If <c>null</c> a different IV
        ///     will be generated for each message and prepended to the actual message payload.
        /// </param>
        /// <returns>
        ///     The endpoint builder so that additional calls can be chained.
        /// </returns>
        public static TBuilder EncryptUsingAes<TBuilder>(
            this IProducerEndpointBuilder<TBuilder> endpointBuilder,
            byte[] key,
            byte[]? initializationVector = null)
            where TBuilder : IProducerEndpointBuilder<TBuilder>
        {
            Check.NotNull(endpointBuilder, nameof(endpointBuilder));

            endpointBuilder.Encrypt(
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
