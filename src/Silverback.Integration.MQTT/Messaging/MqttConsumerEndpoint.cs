// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using Silverback.Messaging.Configuration.Mqtt;

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a topic to consume from.
    /// </summary>
    public sealed class MqttConsumerEndpoint : ConsumerEndpoint, IEquatable<MqttConsumerEndpoint>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttConsumerEndpoint" /> class.
        /// </summary>
        /// <param name="topics">
        ///     The name of the topics or the topic filter strings.
        /// </param>
        public MqttConsumerEndpoint(params string[] topics)
            : base(string.Empty)
        {
            Topics = topics;

            if (topics == null || topics.Length == 0)
                return;

            Name = topics.Length > 1 ? "[" + string.Join(",", topics) + "]" : topics[0];
        }

        /// <summary>
        ///     Gets the name of the topics or the topic filter strings.
        /// </summary>
        public IReadOnlyCollection<string> Topics { get; }

        /// <summary>
        ///     Gets or sets the MQTT client configuration. This is actually a wrapper around the
        ///     <see cref="MqttClientOptions" /> from the MQTTnet library.
        /// </summary>
        public MqttClientConfig Configuration { get; set; } = new();

        /// <summary>
        ///     Gets or sets the quality of service level (at most once, at least once or exactly once).
        /// </summary>
        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        /// <inheritdoc cref="Endpoint.Validate" />
        public override void Validate()
        {
            base.Validate();

            if (Configuration == null)
                throw new EndpointConfigurationException("Configuration cannot be null.");

            Configuration.Validate();
        }

        /// <inheritdoc cref="ConsumerEndpoint.GetUniqueConsumerGroupName" />
        public override string GetUniqueConsumerGroupName() => Name;

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(MqttConsumerEndpoint? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return BaseEquals(other) &&
                   Equals(Configuration, other.Configuration) &&
                   Equals(QualityOfServiceLevel, other.QualityOfServiceLevel);
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

            return Equals((MqttConsumerEndpoint)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = "Protected set is not abused")]
        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
    }
}
