// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging
{
    public class RabbitEndpoint : IEndpoint
    {
        protected RabbitEndpoint(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the queue name.
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

        protected bool Equals(RabbitEndpoint other) => 
            string.Equals(Name, other.Name, StringComparison.InvariantCulture) &&
            Equals(GetJsonString(Serializer), GetJsonString(other.Serializer));

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is RabbitEndpoint other && Equals(other);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode() => Name != null ? StringComparer.InvariantCulture.GetHashCode(Name) : 0;

        private string GetJsonString(object obj) =>
            JsonConvert.SerializeObject(
                obj,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });

        #endregion    
    }
}