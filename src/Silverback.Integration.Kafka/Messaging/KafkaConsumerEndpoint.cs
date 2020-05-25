// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Configuration;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    /// <summary> Represents a topic to consume from. </summary>
    public sealed class KafkaConsumerEndpoint : ConsumerEndpoint, IEquatable<KafkaConsumerEndpoint>
    {
        /// <summary> Initializes a new instance of the <see cref="KafkaConsumerEndpoint" /> class. </summary>
        /// <param name="names"> The names of the topics. </param>
        public KafkaConsumerEndpoint(params string[] names)
            : base(string.Empty)
        {
            Names = names;

            if (names == null)
                return;

            Name = names.Length > 1 ? "[" + string.Join(",", names) + "]" : names[0];
        }

        /// <summary> Gets the names of the topics. </summary>
        public IReadOnlyCollection<string> Names { get; }

        /// <summary>
        ///     Gets or sets the Kafka client configuration. This is actually an extension of the configuration
        ///     dictionary provided by the Confluent.Kafka library.
        /// </summary>
        public KafkaConsumerConfig Configuration { get; set; } = new KafkaConsumerConfig();

        /// <inheritdoc />
        public override void Validate()
        {
            base.Validate();

            if (Configuration == null)
                throw new EndpointConfigurationException("Configuration cannot be null.");

            Configuration.Validate();
        }

        /// <inheritdoc />
        public override string GetUniqueConsumerGroupName() =>
            !string.IsNullOrEmpty(Configuration.GroupId)
                ? Configuration.GroupId
                : Name;

        /// <inheritdoc />
        public bool Equals(KafkaConsumerEndpoint? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return base.Equals(other) && Equals(Configuration, other.Configuration);
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

            return Equals((KafkaConsumerEndpoint)obj);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = "Protected set is not abused")]
        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
    }
}
