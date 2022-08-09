// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a topic to produce to.
    /// </summary>
    public sealed class MqttProducerEndpoint : ProducerEndpoint, IEquatable<MqttProducerEndpoint>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttProducerEndpoint" /> class.
        /// </summary>
        /// <param name="topicName">
        ///     The name of the topic.
        /// </param>
        public MqttProducerEndpoint(string topicName)
            : base(topicName)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttProducerEndpoint" /> class.
        /// </summary>
        /// <param name="topicNameFunction">
        ///     The function returning the topic name for the message being produced. If the function returns
        ///     <c>null</c> the message will not be produced.
        /// </param>
        public MqttProducerEndpoint(Func<IOutboundEnvelope, string?> topicNameFunction)
            : base(topicNameFunction)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttProducerEndpoint" /> class.
        /// </summary>
        /// <param name="topicNameFunction">
        ///     The function returning the topic name for the message being produced. If the function returns
        ///     <c>null</c> the message will not be produced.
        /// </param>
        public MqttProducerEndpoint(Func<IOutboundEnvelope, IServiceProvider, string?> topicNameFunction)
            : base(topicNameFunction)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttProducerEndpoint" /> class.
        /// </summary>
        /// <param name="topicNameFormatString">
        ///     The endpoint name format string that will be combined with the arguments returned by the
        ///     <paramref name="topicNameArgumentsFunction" /> using a <c>string.Format</c>.
        /// </param>
        /// <param name="topicNameArgumentsFunction">
        ///     The function returning the arguments to be used to format the string.
        /// </param>
        public MqttProducerEndpoint(
            string topicNameFormatString,
            Func<IOutboundEnvelope, string[]> topicNameArgumentsFunction)
            : base(topicNameFormatString, topicNameArgumentsFunction)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttProducerEndpoint" /> class.
        /// </summary>
        /// <param name="resolverType">
        ///     The type of the <see cref="IProducerEndpointNameResolver" /> to be used to resolve the actual
        ///     endpoint name.
        /// </param>
        public MqttProducerEndpoint(Type resolverType)
            : base(resolverType)
        {
        }

        /// <summary>
        ///     Gets or sets the MQTT client configuration. This is actually a wrapper around the
        ///     <see cref="MqttClientOptions" /> from the MQTTnet library.
        /// </summary>
        public MqttClientConfig Configuration { get; set; } = new();

        /// <summary>
        ///     Gets or sets the quality of service level (at most once, at least once or exactly once).
        /// </summary>
        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the message have to be sent with the retain flag, causing them
        ///     to be persisted on the broker.
        /// </summary>
        public bool Retain { get; set; }

        /// <summary>
        ///     Gets or sets the message expiry interval in seconds. This interval defines the period of time that the
        ///     broker stores
        ///     the <i>PUBLISH</i> message for any matching subscribers that are not currently connected. When no
        ///     message expiry interval is set, the broker must store the message for matching subscribers
        ///     indefinitely.
        /// </summary>
        public uint? MessageExpiryInterval { get; set; }

        /// <inheritdoc cref="ProducerEndpoint.Validate" />
        public override void Validate()
        {
            base.Validate();

            if (Configuration == null)
                throw new EndpointConfigurationException("Configuration cannot be null.");

            if (Chunk != null)
                throw new EndpointConfigurationException("Chunking is not supported over MQTT.");

            Configuration.Validate();
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(MqttProducerEndpoint? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return BaseEquals(other) &&
                   Equals(Configuration, other.Configuration) &&
                   QualityOfServiceLevel == other.QualityOfServiceLevel;
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

            return Equals((MqttProducerEndpoint)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        [SuppressMessage(
            "ReSharper",
            "NonReadonlyMemberInGetHashCode",
            Justification = "Protected set is not abused")]
        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
    }
}
