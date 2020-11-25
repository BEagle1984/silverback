// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Outbound;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     The base class for the builders of the types inheriting from <see cref="ConsumerEndpoint" />.
    /// </summary>
    /// <typeparam name="TEndpoint">
    ///     The type of the endpoint being built.
    /// </typeparam>
    /// <typeparam name="TBuilder">
    ///     The actual builder type.
    /// </typeparam>
    public abstract class ProducerEndpointBuilder<TEndpoint, TBuilder> : EndpointBuilder<TEndpoint, TBuilder>, IProducerEndpointBuilder<TBuilder>
        where TEndpoint : ProducerEndpoint
        where TBuilder : IProducerEndpointBuilder<TBuilder>
    {
        private IProduceStrategy? _strategy;

        private int? _chunkSize;

        private bool? _alwaysAddChunkHeaders;

        /// <inheritdoc cref="IProducerEndpointBuilder{TBuilder}.SerializeUsing" />
        public TBuilder SerializeUsing(IMessageSerializer serializer) => UseSerializer(serializer);

        /// <inheritdoc cref="IProducerEndpointBuilder{TBuilder}.Encrypt" />
        public TBuilder Encrypt(EncryptionSettings encryptionSettings) => WithEncryption(encryptionSettings);

        /// <inheritdoc cref="IProducerEndpointBuilder{TBuilder}.UseStrategy" />
        public TBuilder UseStrategy(IProduceStrategy strategy)
        {
            _strategy = Check.NotNull(strategy, nameof(strategy));
            return This;
        }

        /// <inheritdoc cref="IProducerEndpointBuilder{TBuilder}.ProduceDirectly" />
        public TBuilder ProduceDirectly()
        {
            _strategy = new DefaultProduceStrategy();
            return This;
        }

        /// <inheritdoc cref="IProducerEndpointBuilder{TBuilder}.ProduceToOutbox" />
        public TBuilder ProduceToOutbox()
        {
            _strategy = new OutboxProduceStrategy();
            return This;
        }

        /// <inheritdoc cref="IProducerEndpointBuilder{TBuilder}.EnableChunking" />
        public TBuilder EnableChunking(int chunkSize, bool alwaysAddHeaders = true)
        {
            if (chunkSize <= 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(chunkSize),
                    chunkSize,
                    "chunkSize must be greater or equal to 1.");
            }

            _chunkSize = chunkSize;
            _alwaysAddChunkHeaders = alwaysAddHeaders;

            return This;
        }

        /// <inheritdoc cref="EndpointBuilder{TEndpoint,TBuilder}.Build" />
        public override TEndpoint Build()
        {
            var endpoint = base.Build();

            if (_strategy != null)
                endpoint.Strategy = _strategy;

            if (_chunkSize != null)
            {
                endpoint.Chunk = new ChunkSettings
                {
                    Size = _chunkSize.Value,
                    AlwaysAddHeaders = _alwaysAddChunkHeaders ?? false
                };
            }

            return endpoint;
        }
    }
}
