// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Primitives;
using Silverback.Messaging.Configuration;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a topic to consume from.
    /// </summary>
    public sealed class KafkaConsumerEndpoint : ConsumerEndpoint, IEquatable<KafkaConsumerEndpoint>
    {
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private string[]? _names;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaConsumerEndpoint" /> class.
        /// </summary>
        public KafkaConsumerEndpoint()
        {
            Names = Array.Empty<string>();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaConsumerEndpoint" /> class.
        /// </summary>
        /// <param name="names">
        ///     The names of the topics.
        /// </param>
        public KafkaConsumerEndpoint(params string[] names)
            : base(string.Empty)
        {
            Names = names;
        }

        /// <inheritdoc cref="IEndpoint.Name" />
        public new string Name
        {
            get => base.Name;
            set { Names = new[] { value }; }
        }

        /// <summary>
        ///     Gets or sets the names of the topics.
        /// </summary>
        public IReadOnlyCollection<string> Names
        {
            get => _names ?? StringValues.Empty;
            set
            {
                _names = value.ToArray();

                if (_names == null || _names.Length == 0)
                    base.Name = string.Empty;
                else if (_names.Length == 1)
                    base.Name = _names[0];
                else
                    base.Name = "[" + string.Join(",", _names) + "]";
            }
        }

        /// <summary>
        ///     Gets or sets the Kafka client configuration. This is actually an extension of the configuration
        ///     dictionary provided by the Confluent.Kafka library.
        /// </summary>
        public KafkaConsumerConfig Configuration { get; set; } = new KafkaConsumerConfig();

        /// <inheritdoc cref="Endpoint.Validate" />
        public override void Validate()
        {
            base.Validate();

            if (Configuration == null)
                throw new EndpointConfigurationException("Configuration cannot be null.");

            Configuration.Validate();
        }

        /// <inheritdoc cref="ConsumerEndpoint.GetUniqueConsumerGroupName" />
        public override string GetUniqueConsumerGroupName() =>
            !string.IsNullOrEmpty(Configuration.GroupId)
                ? Configuration.GroupId
                : Name;

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(KafkaConsumerEndpoint? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return BaseEquals(other) && Equals(Configuration, other.Configuration);
        }

        /// <inheritdoc cref="object.Equals(object)" />
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

        /// <inheritdoc cref="object.GetHashCode" />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = "Protected set is not abused")]
        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
    }
}
