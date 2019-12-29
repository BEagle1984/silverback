// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging
{
    public abstract class Endpoint : IEndpoint
    {
        protected Endpoint(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of the endpoint (being it a queue, topic, exchange, ...).
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets or sets an instance of <see cref="IMessageSerializer"/> to be used to serialize or deserialize
        /// the messages being produced or consumed.
        /// </summary>
        public IMessageSerializer Serializer { get; set; } = DefaultSerializer;

        public static IMessageSerializer DefaultSerializer { get; } = new JsonMessageSerializer();

        /// <inheritdoc cref="IEndpoint"/>
        public virtual void Validate()
        {
            if (string.IsNullOrEmpty(Name))
                throw new EndpointConfigurationException("Name cannot be empty.");

            if (Serializer == null)
                throw new EndpointConfigurationException("Serializer cannot be null");
            
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