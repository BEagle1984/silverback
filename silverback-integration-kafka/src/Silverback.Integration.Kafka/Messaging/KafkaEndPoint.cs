// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging
{
    public abstract class KafkaEndpoint : IEndpoint
    {
        protected KafkaEndpoint(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the topic name(s).
        /// </summary>
        public string Name { get; protected set; }

        public IMessageSerializer Serializer { get; set; } = DefaultSerializer;

        public static IMessageSerializer DefaultSerializer { get; } = new JsonMessageSerializer();

        public virtual void Validate()
        {
            if (string.IsNullOrEmpty(Name))
                throw new EndpointConfigurationException("Name cannot be empty.");

            if (Serializer == null)
                throw new EndpointConfigurationException("Serializer cannot be null");
        }

        #region Equality

        protected bool Equals(KafkaEndpoint other)
        {
            return string.Equals(Name, other.Name, StringComparison.InvariantCulture) && Equals(Serializer, other.Serializer);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is KafkaEndpoint other && Equals(other);
        }

        public override int GetHashCode()=> Name != null ? StringComparer.InvariantCulture.GetHashCode(Name) : 0;

        #endregion
    }
}
