// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging
{
    /// <inheritdoc cref="IEndpoint" />
    public abstract class Endpoint : IEndpoint
    {
        protected Endpoint(string name)
        {
            Name = name;
        }

        public string Name { get; protected set; }

        /// <summary>
        ///     Gets or sets an instance of <see cref="IMessageSerializer" /> to be used to serialize or deserialize
        ///     the messages being produced or consumed.
        /// </summary>
        public IMessageSerializer Serializer { get; set; } = DefaultSerializer;

        /// <summary>
        ///     <para>
        ///         Gets or sets the encryption settings. This optional settings enables the end-to-end message encryption.
        ///     </para>
        ///     <para>
        ///         When enabled the messages are transparently encrypted by the producer and decrypted by the consumer.
        ///     </para>
        ///     <para>
        ///         Set it to <c>null</c> (default) to disable this feature.
        ///     </para>
        /// </summary>
        public EncryptionSettings Encryption { get; set; }

        /// <summary>
        ///     Gets the default serializer instance (a <see cref="JsonMessageSerializer" /> with default settings).
        /// </summary>
        public static IMessageSerializer DefaultSerializer { get; } = JsonMessageSerializer.Default;

        public virtual void Validate()
        {
            if (string.IsNullOrEmpty(Name))
                throw new EndpointConfigurationException("Name cannot be empty.");

            if (Serializer == null)
                throw new EndpointConfigurationException("Serializer cannot be null");

            Encryption?.Validate();
        }

        #region Equality

        protected bool Equals(Endpoint other) =>
            Name == other.Name &&
            ComparisonHelper.JsonEquals(Serializer, other.Serializer);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Endpoint) obj);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode() => Name != null ? Name.GetHashCode() : 0;

        #endregion
    }
}