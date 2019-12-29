// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    public sealed class KafkaProducerEndpoint : ProducerEndpoint, IEquatable<KafkaProducerEndpoint>
    {
        public KafkaProducerEndpoint(string name) : base(name)
        {
        }
        
        /// <summary>
        /// Gets or sets the Kafka client configuration. This is actually an extension of the configuration
        /// dictionary provided by the Confluent.Kafka library.
        /// </summary>
        public KafkaProducerConfig Configuration { get; set; } = new KafkaProducerConfig();

        public override void Validate()
        {
            base.Validate();

            if (Configuration == null)
                throw new EndpointConfigurationException("Configuration cannot be null.");

            Configuration.Validate();
        }

        #region Equality

        public bool Equals(KafkaProducerEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && Equals(Configuration, other.Configuration);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((KafkaProducerEndpoint) obj);
        }

        #endregion
    }
}