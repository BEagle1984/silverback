// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Encryption;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging
{
    /// <inheritdoc cref="IEndpoint" />
    public abstract class Endpoint : IEndpoint
    {
        private string _name;

        private string? _friendlyName;

        private string? _displayName;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Endpoint" /> class.
        /// </summary>
        /// <param name="name">
        ///     The endpoint name.
        /// </param>
        protected Endpoint(string name)
        {
            _name = name;
        }

        /// <summary>
        ///     Gets the default serializer (a <see cref="JsonMessageSerializer" /> with default settings).
        /// </summary>
        public static IMessageSerializer DefaultSerializer => JsonMessageSerializer.Default;

        /// <inheritdoc cref="IEndpoint.DisplayName" />
        public string DisplayName => _displayName ?? _name;

        /// <inheritdoc cref="IEndpoint.Name" />
        public string Name
        {
            get => _name;

            protected set
            {
                _name = value;
                UpdateDisplayName();
            }
        }

        /// <summary>
        ///     Gets or sets an optional friendly name to be used to identify in the endpoint. This name will primarily
        ///     be used to compose the <see cref="DisplayName" /> and it will be shown in the human-targeted output (e.g.
        ///     logs, health checks result, etc.).
        /// </summary>
        public string? FriendlyName
        {
            get => _friendlyName;

            set
            {
                _friendlyName = string.IsNullOrWhiteSpace(value) || value == _name ? null : value;
                UpdateDisplayName();
            }
        }

        /// <summary>
        ///     Gets or sets the <see cref="IMessageSerializer" /> to be used to serialize or deserialize the messages
        ///     being produced or consumed.
        /// </summary>
        public IMessageSerializer Serializer { get; set; } = DefaultSerializer;

        /// <summary>
        ///     <para>
        ///         Gets or sets the encryption settings. This optional settings enables the end-to-end message
        ///         encryption.
        ///     </para>
        ///     <para>
        ///         When enabled the messages are transparently encrypted by the producer and decrypted by the
        ///         consumer.
        ///     </para>
        ///     <para>
        ///         Set it to <c>null</c> (default) to disable this feature.
        ///     </para>
        /// </summary>
        public EncryptionSettings? Encryption { get; set; }

        /// <inheritdoc cref="IEndpoint.Validate" />
        public virtual void Validate()
        {
            if (string.IsNullOrEmpty(Name))
                throw new EndpointConfigurationException("Name cannot be empty.");

            if (Serializer == null)
                throw new EndpointConfigurationException("Serializer cannot be null");

            Encryption?.Validate();
        }

        /// <summary>
        ///     Determines whether the specified <see cref="Endpoint" /> is equal to the current
        ///     <see cref="Endpoint" />.
        /// </summary>
        /// <param name="other">
        ///     The object to compare with the current object.
        /// </param>
        /// <returns>
        ///     Returns a value indicating whether the other object is equal to the current object.
        /// </returns>
        protected virtual bool BaseEquals(Endpoint? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Name == other.Name && DisplayName == other.DisplayName &&
                   Equals(Serializer, other.Serializer);
        }

        private void UpdateDisplayName()
        {
            _displayName = _friendlyName != null ? $"{_friendlyName} [{_name}]" : null;
        }
    }
}
