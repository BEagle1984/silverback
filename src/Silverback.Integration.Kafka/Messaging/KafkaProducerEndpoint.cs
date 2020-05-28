// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a topic to produce to.
    /// </summary>
    public sealed class KafkaProducerEndpoint : ProducerEndpoint, IEquatable<KafkaProducerEndpoint>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaProducerEndpoint" /> class.
        /// </summary>
        /// <param name="name">
        ///     The name of the topic.
        /// </param>
        public KafkaProducerEndpoint(string name)
            : base(name)
        {
        }

        /// <summary>
        ///     Gets or sets the Kafka client configuration. This is actually an extension of the configuration
        ///     dictionary provided by the Confluent.Kafka library.
        /// </summary>
        public KafkaProducerConfig Configuration { get; set; } = new KafkaProducerConfig();

        /// <inheritdoc />
        public override void Validate()
        {
            base.Validate();

            if (Configuration == null)
                throw new EndpointConfigurationException("Configuration cannot be null.");

            Configuration.Validate();
        }

        /// <inheritdoc />
        public bool Equals(KafkaProducerEndpoint? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return BaseEquals(other) && Equals(Configuration, other.Configuration);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((KafkaProducerEndpoint)obj);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = "Protected set is not abused")]
        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
    }
}
