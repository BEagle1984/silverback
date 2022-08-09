// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a topic to produce to.
    /// </summary>
    public sealed class KafkaProducerEndpoint : ProducerEndpoint, IEquatable<KafkaProducerEndpoint>
    {
        private static readonly Func<IOutboundEnvelope, IServiceProvider, Partition> AnyPartitionFunction =
            (_, _) => Partition.Any;

        private readonly Func<IOutboundEnvelope, IServiceProvider, Partition> _partitionFunction;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaProducerEndpoint" /> class.
        /// </summary>
        /// <param name="topicName">
        ///     The name of the topic.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaProducerConfig" />.
        /// </param>
        public KafkaProducerEndpoint(
            string topicName,
            KafkaClientConfig? clientConfig = null)
            : this(topicName, (int?)null, clientConfig)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaProducerEndpoint" /> class.
        /// </summary>
        /// <param name="topicName">
        ///     The name of the topic.
        /// </param>
        /// <param name="partition">
        ///     The optional partition index. If <c>null</c> the partition is automatically derived from the message
        ///     key (use <see cref="KafkaKeyMemberAttribute" /> to specify a message key, otherwise a random one will be
        ///     generated).
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaProducerConfig" />.
        /// </param>
        public KafkaProducerEndpoint(
            string topicName,
            int? partition,
            KafkaClientConfig? clientConfig = null)
            : base(topicName)
        {
            if (partition != null && partition < Partition.Any)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(partition),
                    "The partition index must be greater or equal to 0, or Partition.Any (-1).");
            }

            Configuration = new KafkaProducerConfig(clientConfig);

            if (partition != null)
                _partitionFunction = (_, _) => partition.Value;
            else
                _partitionFunction = AnyPartitionFunction;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaProducerEndpoint" /> class.
        /// </summary>
        /// <param name="topicNameFunction">
        ///     The function returning the topic name for the message being produced. If the function returns
        ///     <c>null</c> the message will not be produced.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaProducerConfig" />.
        /// </param>
        public KafkaProducerEndpoint(
            Func<IOutboundEnvelope, string?> topicNameFunction,
            KafkaClientConfig? clientConfig = null)
            : this(topicNameFunction, null, clientConfig)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaProducerEndpoint" /> class.
        /// </summary>
        /// <param name="topicNameFunction">
        ///     The function returning the topic name for the message being produced. If the function returns
        ///     <c>null</c> the message will not be produced.
        /// </param>
        /// <param name="partitionFunction">
        ///     The optional function returning the target partition index for the message being produced. If <c>null</c>
        ///     the partition is automatically derived from the message key (use <see cref="KafkaKeyMemberAttribute" />
        ///     to specify a message key, otherwise a random one will be generated).
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaProducerConfig" />.
        /// </param>
        public KafkaProducerEndpoint(
            Func<IOutboundEnvelope, string?> topicNameFunction,
            Func<IOutboundEnvelope, int>? partitionFunction,
            KafkaClientConfig? clientConfig = null)
            : base(topicNameFunction)
        {
            Configuration = new KafkaProducerConfig(clientConfig);

            if (partitionFunction != null)
                _partitionFunction = (envelope, _) => partitionFunction.Invoke(envelope);
            else
                _partitionFunction = AnyPartitionFunction;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaProducerEndpoint" /> class.
        /// </summary>
        /// <param name="topicNameFunction">
        ///     The function returning the topic name for the message being produced. If the function returns
        ///     <c>null</c> the message will not be produced.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaProducerConfig" />.
        /// </param>
        public KafkaProducerEndpoint(
            Func<IOutboundEnvelope, IServiceProvider, string?> topicNameFunction,
            KafkaClientConfig? clientConfig = null)
            : this(topicNameFunction, null, clientConfig)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaProducerEndpoint" /> class.
        /// </summary>
        /// <param name="topicNameFunction">
        ///     The function returning the topic name for the message being produced. If the function returns
        ///     <c>null</c> the message will not be produced.
        /// </param>
        /// <param name="partitionFunction">
        ///     The optional function returning the target partition index for the message being produced. If <c>null</c>
        ///     the partition is automatically derived from the message key (use <see cref="KafkaKeyMemberAttribute" />
        ///     to specify a message key, otherwise a random one will be generated).
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaProducerConfig" />.
        /// </param>
        public KafkaProducerEndpoint(
            Func<IOutboundEnvelope, IServiceProvider, string?> topicNameFunction,
            Func<IOutboundEnvelope, IServiceProvider, int>? partitionFunction,
            KafkaClientConfig? clientConfig = null)
            : base(topicNameFunction)
        {
            Configuration = new KafkaProducerConfig(clientConfig);

            if (partitionFunction != null)
            {
                _partitionFunction =
                    (envelope, serviceProvider) => partitionFunction.Invoke(envelope, serviceProvider);
            }
            else
            {
                _partitionFunction = AnyPartitionFunction;
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaProducerEndpoint" /> class.
        /// </summary>
        /// <param name="topicNameFormatString">
        ///     The endpoint name format string that will be combined with the arguments returned by the
        ///     <paramref name="topicNameArgumentsFunction" /> using a <c>string.Format</c>.
        /// </param>
        /// <param name="topicNameArgumentsFunction">
        ///     The function returning the arguments to be used to format the string.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaProducerConfig" />.
        /// </param>
        [SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Read-only array")]
        public KafkaProducerEndpoint(
            string topicNameFormatString,
            Func<IOutboundEnvelope, string[]> topicNameArgumentsFunction,
            KafkaClientConfig? clientConfig = null)
            : this(topicNameFormatString, topicNameArgumentsFunction, null, clientConfig)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaProducerEndpoint" /> class.
        /// </summary>
        /// <param name="topicNameFormatString">
        ///     The endpoint name format string that will be combined with the arguments returned by the
        ///     <paramref name="topicNameArgumentsFunction" /> using a <c>string.Format</c>.
        /// </param>
        /// <param name="topicNameArgumentsFunction">
        ///     The function returning the arguments to be used to format the string.
        /// </param>
        /// <param name="partitionFunction">
        ///     The optional function returning the target partition index for the message being produced. If <c>null</c>
        ///     the partition is automatically derived from the message key (use <see cref="KafkaKeyMemberAttribute" />
        ///     to specify a message key, otherwise a random one will be generated).
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaProducerConfig" />.
        /// </param>
        [SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Read-only array")]
        public KafkaProducerEndpoint(
            string topicNameFormatString,
            Func<IOutboundEnvelope, string[]> topicNameArgumentsFunction,
            Func<IOutboundEnvelope, int>? partitionFunction,
            KafkaClientConfig? clientConfig = null)
            : base(topicNameFormatString, topicNameArgumentsFunction)
        {
            Configuration = new KafkaProducerConfig(clientConfig);

            if (partitionFunction != null)
                _partitionFunction = (envelope, _) => partitionFunction.Invoke(envelope);
            else
                _partitionFunction = AnyPartitionFunction;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaProducerEndpoint" /> class.
        /// </summary>
        /// <param name="resolverType">
        ///     The type of the <see cref="IKafkaProducerEndpointNameResolver" /> to be used to resolve the actual
        ///     endpoint name.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaProducerConfig" />.
        /// </param>
        public KafkaProducerEndpoint(Type resolverType, KafkaClientConfig? clientConfig = null)
            : base(resolverType)
        {
            Configuration = new KafkaProducerConfig(clientConfig);

            if (!typeof(IKafkaProducerEndpointNameResolver).IsAssignableFrom(resolverType))
            {
                throw new ArgumentException(
                    "The specified type must implement IKafkaProducerEndpointNameResolver.",
                    nameof(resolverType));
            }

            _partitionFunction = (envelope, serviceProvider) =>
                ((IKafkaProducerEndpointNameResolver)serviceProvider.GetRequiredService(resolverType))
                .GetPartition(envelope) ?? Partition.Any;
        }

        /// <summary>
        ///     Gets or sets the Kafka client configuration. This is actually an extension of the configuration
        ///     dictionary provided by the Confluent.Kafka library.
        /// </summary>
        public KafkaProducerConfig Configuration { get; set; }

        /// <summary>
        ///     Gets the target partition. When set to <c>Partition.Any</c> (-1) the partition is automatically
        ///     derived from the message key (use <see cref="KafkaKeyMemberAttribute" /> to specify a message key,
        ///     otherwise a random one will be generated).
        ///     The default is <c>Partition.Any</c> (-1).
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message being produced.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> in the current scope.
        /// </param>
        /// <returns>
        ///     The partition to be produced to.
        /// </returns>
        public Partition GetPartition(IOutboundEnvelope envelope, IServiceProvider serviceProvider) =>
            _partitionFunction.Invoke(envelope, serviceProvider);

        /// <inheritdoc cref="ProducerEndpoint.Validate" />
        public override void Validate()
        {
            base.Validate();

            if (Configuration == null)
                throw new EndpointConfigurationException("Configuration cannot be null.");

            Configuration.Validate();
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(KafkaProducerEndpoint? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return BaseEquals(other) &&
                   Equals(Configuration, other.Configuration);
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

            return Equals((KafkaProducerEndpoint)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        [SuppressMessage(
            "ReSharper",
            "NonReadonlyMemberInGetHashCode",
            Justification = "Protected set is not abused")]
        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
    }
}
