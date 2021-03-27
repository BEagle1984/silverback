// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Util;

namespace Silverback.Messaging
{
    /// <inheritdoc cref="IProducerEndpoint" />
    public abstract class ProducerEndpoint : Endpoint, IProducerEndpoint
    {
        private readonly Func<IOutboundEnvelope, IServiceProvider, string> _nameFunction;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProducerEndpoint" /> class.
        /// </summary>
        /// <param name="name">
        ///     The endpoint name.
        /// </param>
        protected ProducerEndpoint(string name)
            : base(name) =>
            _nameFunction = (_, _) => name;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProducerEndpoint" /> class.
        /// </summary>
        /// <param name="nameFunction">
        ///     The function returning the endpoint name for the message being produced.
        /// </param>
        protected ProducerEndpoint(Func<IOutboundEnvelope, string> nameFunction)
            : base("[dynamic]") =>
            _nameFunction = (envelope, _) => nameFunction.Invoke(envelope);

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProducerEndpoint" /> class.
        /// </summary>
        /// <param name="nameFunction">
        ///     The function returning the endpoint name for the message being produced.
        /// </param>
        protected ProducerEndpoint(Func<IOutboundEnvelope, IServiceProvider, string> nameFunction)
            : base("[dynamic]")
        {
            Check.NotNull(nameFunction, nameof(nameFunction));

            _nameFunction = nameFunction.Invoke;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProducerEndpoint" /> class.
        /// </summary>
        /// <param name="nameFormat">
        ///     The endpoint name format string that will be combined with the arguments returned by the
        ///     <paramref name="argumentsFunction" /> using a <c>string.Format</c>.
        /// </param>
        /// <param name="argumentsFunction">
        ///     The function returning the arguments to be used to format the string.
        /// </param>
        [SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Read-only array")]
        protected ProducerEndpoint(string nameFormat, Func<IOutboundEnvelope, string[]> argumentsFunction)
            : base(nameFormat) =>
            _nameFunction = (envelope, _) => string.Format(
                CultureInfo.InvariantCulture,
                nameFormat,
                argumentsFunction.Invoke(envelope));

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProducerEndpoint" /> class.
        /// </summary>
        /// <param name="nameResolverType">
        ///     The type of the <see cref="IProducerEndpointNameResolver" /> to be used to resolve the actual endpoint
        ///     name.
        /// </param>
        protected ProducerEndpoint(Type nameResolverType)
            : base($"[dynamic | {nameResolverType}]")
        {
            if (!typeof(IProducerEndpointNameResolver).IsAssignableFrom(nameResolverType))
            {
                throw new ArgumentException(
                    "The specified type must implement IProducerEndpointNameResolver.",
                    nameof(nameResolverType));
            }

            _nameFunction = (envelope, serviceProvider) =>
                ((IProducerEndpointNameResolver)serviceProvider.GetRequiredService(nameResolverType))
                .GetName(envelope);
        }

        /// <summary>
        ///     Gets or sets the message chunking settings. This option can be used to split large messages into
        ///     smaller chunks.
        /// </summary>
        public ChunkSettings? Chunk { get; set; }

        /// <summary>
        ///     Gets or sets the strategy to be used to produce the messages. If no strategy is specified, the
        ///     messages will be sent to the message broker directly.
        /// </summary>
        public IProduceStrategy Strategy { get; set; } = new DefaultProduceStrategy();

        /// <summary>
        ///     Gets or sets the collection of <see cref="IOutboundMessageEnricher" /> to be used to enrich the outbound
        ///     message.
        /// </summary>
        [SuppressMessage("", "CA2227", Justification = "Easier initialization")]
        public ICollection<IOutboundMessageEnricher> MessageEnrichers { get; set; } =
            new List<IOutboundMessageEnricher>();

        /// <inheritdoc cref="IProducerEndpoint.MessageEnrichers" />
        IReadOnlyCollection<IOutboundMessageEnricher> IProducerEndpoint.MessageEnrichers =>
            MessageEnrichers.AsReadOnlyCollection();

        /// <inheritdoc cref="IProducerEndpoint.GetActualName" />
        public string GetActualName(IOutboundEnvelope envelope, IServiceProvider serviceProvider) =>
            _nameFunction.Invoke(envelope, serviceProvider);

        /// <inheritdoc cref="Endpoint.Validate" />
        public override void Validate()
        {
            base.Validate();

            if (Strategy == null)
                throw new EndpointConfigurationException("Strategy cannot be null.");

            Chunk?.Validate();
        }

        /// <inheritdoc cref="Endpoint.BaseEquals" />
        protected override bool BaseEquals(Endpoint? other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other is not ProducerEndpoint otherProducerEndpoint)
                return false;

            return base.BaseEquals(other) && Equals(Chunk, otherProducerEndpoint.Chunk);
        }
    }
}
