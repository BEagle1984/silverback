// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Encryption;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     The base class for the builders of the types inheriting from <see cref="Endpoint" />.
    /// </summary>
    /// <typeparam name="TEndpoint">
    ///     The type of the endpoint being built.
    /// </typeparam>
    /// <typeparam name="TBuilder">
    ///     The actual builder type.
    /// </typeparam>
    public abstract class EndpointBuilder<TEndpoint, TBuilder> : IEndpointBuilder<TBuilder>
        where TEndpoint : Endpoint
        where TBuilder : IEndpointBuilder<TBuilder>
    {
        private IMessageSerializer? _serializer;

        private EncryptionSettings? _encryptionSettings;

        /// <summary>
        ///     Gets this instance.
        /// </summary>
        /// <remarks>
        ///     This is necessary to work around casting in the base classes.
        /// </remarks>
        protected abstract TBuilder This { get; }

        /// <inheritdoc cref="IEndpointBuilder{TBuilder}.UseSerializer" />
        public TBuilder UseSerializer(IMessageSerializer serializer)
        {
            _serializer = Check.NotNull(serializer, nameof(serializer));
            return This;
        }

        /// <inheritdoc cref="IEndpointBuilder{TBuilder}.WithEncryption" />
        public TBuilder WithEncryption(EncryptionSettings? encryptionSettings)
        {
            _encryptionSettings = encryptionSettings;
            return This;
        }

        /// <summary>
        ///     Builds the endpoint instance.
        /// </summary>
        /// <returns>
        ///     The endpoint.
        /// </returns>
        public virtual TEndpoint Build()
        {
            var endpoint = CreateEndpoint();

            if (_serializer != null)
                endpoint.Serializer = _serializer;

            endpoint.Encryption = _encryptionSettings;

            endpoint.Validate();

            return endpoint;
        }

        /// <summary>
        ///     Creates the endpoint to be configured according to the options stored in the builder.
        /// </summary>
        /// <returns>
        ///     The endpoint.
        /// </returns>
        protected abstract TEndpoint CreateEndpoint();
    }
}
