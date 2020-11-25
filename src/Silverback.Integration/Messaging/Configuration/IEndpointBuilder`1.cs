// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Encryption;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Builds the <see cref="Endpoint"/>.
    /// </summary>
    /// <typeparam name="TBuilder">
    ///     The actual builder type.
    /// </typeparam>
    public interface IEndpointBuilder<out TBuilder>
        where TBuilder : IEndpointBuilder<TBuilder>
    {
        /// <summary>
        ///     Specifies the <see cref="IMessageSerializer"/> to be used serialize or deserialize the messages.
        /// </summary>
        /// <param name="serializer">
        ///    The <see cref="IMessageSerializer"/>.
        /// </param>
        /// <returns>
        ///     The endpoint builder so that additional calls can be chained.
        /// </returns>
        TBuilder UseSerializer(IMessageSerializer serializer);

        /// <summary>
        ///     Enables the end-to-end message encryption.
        /// </summary>
        /// <param name="encryptionSettings">
        ///    The <see cref="EncryptionSettings"/>.
        /// </param>
        /// <returns>
        ///     The endpoint builder so that additional calls can be chained.
        /// </returns>
        TBuilder WithEncryption(EncryptionSettings? encryptionSettings);
    }
}
