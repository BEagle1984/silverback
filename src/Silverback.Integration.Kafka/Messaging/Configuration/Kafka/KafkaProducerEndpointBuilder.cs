// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka
{
    /// <inheritdoc cref="IKafkaProducerEndpointBuilder" />
    public class KafkaProducerEndpointBuilder
        : ProducerEndpointBuilder<KafkaProducerEndpoint, IKafkaProducerEndpointBuilder>,
            IKafkaProducerEndpointBuilder
    {
        private readonly KafkaClientConfig? _clientConfig;

        private readonly List<Action<KafkaProducerConfig>> _configActions = new();

        private Func<KafkaProducerEndpoint>? _endpointFactory;

        private IOutboundMessageEnricher? _kafkaKeyEnricher;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaProducerEndpointBuilder" /> class.
        /// </summary>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaProducerConfig" />.
        /// </param>
        /// <param name="endpointsConfigurationBuilder">
        ///     The optional reference to the <see cref="IEndpointsConfigurationBuilder" /> that instantiated the
        ///     builder.
        /// </param>
        public KafkaProducerEndpointBuilder(
            KafkaClientConfig? clientConfig = null,
            IEndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
            : base(endpointsConfigurationBuilder)
        {
            _clientConfig = clientConfig;
        }

        /// <inheritdoc cref="EndpointBuilder{TEndpoint,TBuilder}.This" />
        protected override IKafkaProducerEndpointBuilder This => this;

        /// <inheritdoc cref="IKafkaProducerEndpointBuilder.ProduceTo(string, int?)" />
        public IKafkaProducerEndpointBuilder ProduceTo(string topicName, int? partition = null)
        {
            Check.NotEmpty(topicName, nameof(topicName));

            _endpointFactory = () => new KafkaProducerEndpoint(topicName, partition, _clientConfig);

            return this;
        }

        /// <inheritdoc cref="IKafkaProducerEndpointBuilder.ProduceTo(Func{IOutboundEnvelope, string}, Func{IOutboundEnvelope, int}?)" />
        public IKafkaProducerEndpointBuilder ProduceTo(
            Func<IOutboundEnvelope, string> topicNameFunction,
            Func<IOutboundEnvelope, int>? partitionFunction = null)
        {
            Check.NotNull(topicNameFunction, nameof(topicNameFunction));

            _endpointFactory = () => new KafkaProducerEndpoint(
                topicNameFunction,
                partitionFunction,
                _clientConfig);

            return this;
        }

        /// <inheritdoc cref="IKafkaProducerEndpointBuilder.ProduceTo{TMessage}(Func{IOutboundEnvelope{TMessage}, string}, Func{IOutboundEnvelope{TMessage}, int}?)" />
        public IKafkaProducerEndpointBuilder ProduceTo<TMessage>(
            Func<IOutboundEnvelope<TMessage>, string> topicNameFunction,
            Func<IOutboundEnvelope<TMessage>, int>? partitionFunction = null)
            where TMessage : class
        {
            Check.NotNull(topicNameFunction, nameof(topicNameFunction));

            Func<IOutboundEnvelope, int>? wrappedPartitionFunction = null;

            if (partitionFunction != null)
            {
                wrappedPartitionFunction =
                    envelope => partitionFunction.Invoke((IOutboundEnvelope<TMessage>)envelope);
            }

            _endpointFactory = () => new KafkaProducerEndpoint(
                envelope => topicNameFunction.Invoke((IOutboundEnvelope<TMessage>)envelope),
                wrappedPartitionFunction,
                _clientConfig);

            return this;
        }

        /// <inheritdoc cref="IKafkaProducerEndpointBuilder.ProduceTo(Func{IOutboundEnvelope, IServiceProvider, string}, Func{IOutboundEnvelope, IServiceProvider, int}?)" />
        public IKafkaProducerEndpointBuilder ProduceTo(
            Func<IOutboundEnvelope, IServiceProvider, string> topicNameFunction,
            Func<IOutboundEnvelope, IServiceProvider, int>? partitionFunction = null)
        {
            Check.NotNull(topicNameFunction, nameof(topicNameFunction));

            _endpointFactory = () => new KafkaProducerEndpoint(
                topicNameFunction,
                partitionFunction,
                _clientConfig);

            return this;
        }

        /// <inheritdoc cref="IKafkaProducerEndpointBuilder.ProduceTo{TMessage}(Func{IOutboundEnvelope{TMessage}, IServiceProvider, string}, Func{IOutboundEnvelope{TMessage}, IServiceProvider, int}?)" />
        public IKafkaProducerEndpointBuilder ProduceTo<TMessage>(
            Func<IOutboundEnvelope<TMessage>, IServiceProvider, string> topicNameFunction,
            Func<IOutboundEnvelope<TMessage>, IServiceProvider, int>? partitionFunction = null)
            where TMessage : class
        {
            Check.NotNull(topicNameFunction, nameof(topicNameFunction));

            Func<IOutboundEnvelope, IServiceProvider, int>? wrappedPartitionFunction = null;

            if (partitionFunction != null)
            {
                wrappedPartitionFunction = (envelope, serviceProvider) =>
                    partitionFunction.Invoke((IOutboundEnvelope<TMessage>)envelope, serviceProvider);
            }

            _endpointFactory = () => new KafkaProducerEndpoint(
                (envelope, serviceProvider) =>
                    topicNameFunction.Invoke((IOutboundEnvelope<TMessage>)envelope, serviceProvider),
                wrappedPartitionFunction,
                _clientConfig);

            return this;
        }

        /// <inheritdoc cref="IKafkaProducerEndpointBuilder.ProduceTo(string, Func{IOutboundEnvelope, string[]}, Func{IOutboundEnvelope, int}?)" />
        public IKafkaProducerEndpointBuilder ProduceTo(
            string topicNameFormatString,
            Func<IOutboundEnvelope, string[]> topicNameArgumentsFunction,
            Func<IOutboundEnvelope, int>? partitionFunction = null)
        {
            Check.NotEmpty(topicNameFormatString, nameof(topicNameFormatString));
            Check.NotNull(topicNameArgumentsFunction, nameof(topicNameArgumentsFunction));

            _endpointFactory = () => new KafkaProducerEndpoint(
                topicNameFormatString,
                topicNameArgumentsFunction,
                partitionFunction,
                _clientConfig);

            return this;
        }

        /// <inheritdoc cref="IKafkaProducerEndpointBuilder.ProduceTo{TMessage}(string, Func{IOutboundEnvelope{TMessage}, string[]}, Func{IOutboundEnvelope{TMessage}, int}?)" />
        public IKafkaProducerEndpointBuilder ProduceTo<TMessage>(
            string topicNameFormatString,
            Func<IOutboundEnvelope<TMessage>, string[]> topicNameArgumentsFunction,
            Func<IOutboundEnvelope<TMessage>, int>? partitionFunction = null)
            where TMessage : class
        {
            Check.NotEmpty(topicNameFormatString, nameof(topicNameFormatString));
            Check.NotNull(topicNameArgumentsFunction, nameof(topicNameArgumentsFunction));

            Func<IOutboundEnvelope, int>? wrappedPartitionFunction = null;

            if (partitionFunction != null)
            {
                wrappedPartitionFunction = envelope =>
                    partitionFunction.Invoke((IOutboundEnvelope<TMessage>)envelope);
            }

            _endpointFactory = () => new KafkaProducerEndpoint(
                topicNameFormatString,
                envelope => topicNameArgumentsFunction.Invoke((IOutboundEnvelope<TMessage>)envelope),
                wrappedPartitionFunction,
                _clientConfig);

            return this;
        }

        /// <inheritdoc cref="IKafkaProducerEndpointBuilder.UseEndpointNameResolver{TResolver}" />
        public IKafkaProducerEndpointBuilder UseEndpointNameResolver<TResolver>()
            where TResolver : IKafkaProducerEndpointNameResolver
        {
            _endpointFactory = () => new KafkaProducerEndpoint(typeof(TResolver), _clientConfig);

            return this;
        }

        /// <inheritdoc cref="IKafkaProducerEndpointBuilder.WithKafkaKey{TMessage}" />
        public IKafkaProducerEndpointBuilder WithKafkaKey<TMessage>(
            Func<IOutboundEnvelope<TMessage>, object?> valueProvider)
            where TMessage : class
        {
            _kafkaKeyEnricher = new OutboundMessageKafkaKeyEnricher<TMessage>(valueProvider);

            return this;
        }

        /// <inheritdoc cref="IKafkaProducerEndpointBuilder.Configure" />
        public IKafkaProducerEndpointBuilder Configure(Action<KafkaProducerConfig> configAction)
        {
            Check.NotNull(configAction, nameof(configAction));

            _configActions.Add(configAction);

            return this;
        }

        /// <inheritdoc cref="EndpointBuilder{TEndpoint,TBuilder}.CreateEndpoint" />
        protected override KafkaProducerEndpoint CreateEndpoint()
        {
            if (_endpointFactory == null)
            {
                throw new EndpointConfigurationException(
                    "Topic name not set. Use ProduceTo or UseEndpointNameResolver to set it.");
            }

            var endpoint = _endpointFactory.Invoke();

            _configActions.ForEach(action => action.Invoke(endpoint.Configuration));

            if (_kafkaKeyEnricher != null)
                endpoint.MessageEnrichers.Add(_kafkaKeyEnricher);

            return endpoint;
        }
    }
}
