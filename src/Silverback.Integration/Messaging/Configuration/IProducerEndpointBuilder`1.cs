// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Encryption;
using Silverback.Messaging.Outbound;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Builds the <see cref="ProducerEndpoint" />.
    /// </summary>
    /// <typeparam name="TBuilder">
    ///     The actual builder type.
    /// </typeparam>
    public interface IProducerEndpointBuilder<out TBuilder> : IEndpointBuilder<TBuilder>
        where TBuilder : IProducerEndpointBuilder<TBuilder>
    {
        /// <summary>
        ///     Specifies the <see cref="IMessageSerializer" /> to be used to serialize the messages.
        /// </summary>
        /// <param name="serializer">
        ///     The <see cref="IMessageSerializer" />.
        /// </param>
        /// <returns>
        ///     The endpoint builder so that additional calls can be chained.
        /// </returns>
        TBuilder SerializeUsing(IMessageSerializer serializer);

        /// <summary>
        ///     Specifies the <see cref="EncryptionSettings" /> to be used to encrypt the messages.
        /// </summary>
        /// <param name="encryptionSettings">
        ///     The <see cref="EncryptionSettings" />.
        /// </param>
        /// <returns>
        ///     The endpoint builder so that additional calls can be chained.
        /// </returns>
        TBuilder Encrypt(EncryptionSettings encryptionSettings);

        /// <summary>
        ///     Specifies the strategy to be used to produce the messages.
        /// </summary>
        /// <param name="strategy">
        ///     The <see cref="IProduceStrategy" />.
        /// </param>
        /// <returns>
        ///     The endpoint builder so that additional calls can be chained.
        /// </returns>
        TBuilder UseStrategy(IProduceStrategy strategy);

        /// <summary>
        ///     Specifies that the <see cref="DefaultProduceStrategy" /> has to be used, producing directly to the
        ///     message broker.
        /// </summary>
        /// <returns>
        ///     The endpoint builder so that additional calls can be chained.
        /// </returns>
        TBuilder ProduceDirectly();

        /// <summary>
        ///     Specifies that the<see cref="OutboxProduceStrategy" /> has to be used, storing the messages into the
        ///     transactional outbox table. The operation is therefore included in the database transaction applying
        ///     the message side effects to the local database. The <see cref="IOutboxWorker" /> takes care of
        ///     asynchronously sending the messages to the message broker.
        /// </summary>
        /// <returns>
        ///     The endpoint builder so that additional calls can be chained.
        /// </returns>
        TBuilder ProduceToOutbox();

        /// <summary>
        ///     Enables chunking, splitting the larger messages into smaller chunks.
        /// </summary>
        /// <param name="chunkSize">
        ///     The maximum chunk size in bytes.
        /// </param>
        /// <param name="alwaysAddHeaders">
        ///     A value indicating whether the <c>x-chunk-index</c> and related headers have to be added to the
        ///     produced message in any case, even if its size doesn't exceed the single chunk size. The default is
        ///     <c>true</c>.
        /// </param>
        /// <returns>
        ///     The endpoint builder so that additional calls can be chained.
        /// </returns>
        TBuilder EnableChunking(int chunkSize, bool alwaysAddHeaders = true);
    }
}
