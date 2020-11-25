﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Producer" />
    public abstract class Producer<TBroker, TEndpoint> : Producer
        where TBroker : IBroker
        where TEndpoint : IProducerEndpoint
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Producer{TBroker,TEndpoint}" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that instantiated this producer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to produce to.
        /// </param>
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        protected Producer(
            TBroker broker,
            TEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<Producer> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
        {
        }

        /// <summary>
        ///     Gets the <typeparamref name="TBroker" /> that owns this producer.
        /// </summary>
        public new TBroker Broker => (TBroker)base.Broker;

        /// <summary>
        ///     Gets the <typeparamref name="TEndpoint" /> representing the endpoint that is being produced to.
        /// </summary>
        public new TEndpoint Endpoint => (TEndpoint)base.Endpoint;
    }
}
