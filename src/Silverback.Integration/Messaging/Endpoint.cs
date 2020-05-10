// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging
{
    /// <inheritdoc cref="IEndpoint" />
    public abstract class Endpoint : IEndpoint
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Endpoint" /> class.
        /// </summary>
        /// <param name="name"> The endpoint name. </param>
        protected Endpoint(string name)
        {
            Name = name;
        }

        /// <summary>
        ///     Gets the default serializer (a <see cref="JsonMessageSerializer" /> with default settings).
        /// </summary>
        public static IMessageSerializer DefaultSerializer { get; } = JsonMessageSerializer.Default;

        /// <inheritdoc />
        public string Name { get; protected set; }

        /// <inheritdoc />
        public IMessageSerializer Serializer { get; set; } = DefaultSerializer;

        /// <inheritdoc />
        public EncryptionSettings? Encryption { get; set; }

        /// <inheritdoc />
        public virtual void Validate()
        {
            if (string.IsNullOrEmpty(Name))
                throw new EndpointConfigurationException("Name cannot be empty.");

            if (Serializer == null)
                throw new EndpointConfigurationException("Serializer cannot be null");

            Encryption?.Validate();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((Endpoint)obj);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = Justifications.Settings)]
        public override int GetHashCode()
        {
            return Name != null ? Name.GetHashCode(StringComparison.InvariantCulture) : 0;
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
        protected bool Equals(Endpoint other)
        {
            return Name == other?.Name &&
                   ComparisonHelper.JsonEquals(Serializer, other.Serializer);
        }
    }
}
