// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Encryption;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging
{
    /// <inheritdoc cref="IEndpoint" />
    public abstract class Endpoint : IEndpoint
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Endpoint" /> class.
        /// </summary>
        /// <param name="name">
        ///     The endpoint name.
        /// </param>
        protected Endpoint(string name)
        {
            Name = name;
        }

        /// <summary>
        ///     Gets the default serializer (a <see cref="JsonMessageSerializer" /> with default settings).
        /// </summary>
        public static IMessageSerializer DefaultSerializer { get; } = JsonMessageSerializer.Default;

        /// <inheritdoc cref="IEndpoint.Name" />
        public string Name { get; protected set; }

        /// <inheritdoc cref="IEndpoint.Serializer" />
        public IMessageSerializer Serializer { get; set; } = DefaultSerializer;

        /// <inheritdoc cref="IEndpoint.Encryption" />
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

            return Name == other.Name && Equals(Serializer, other.Serializer);
        }
    }
}
