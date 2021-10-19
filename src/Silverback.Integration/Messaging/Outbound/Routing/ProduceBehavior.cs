// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.Routing
{
    /// <summary>
    ///     Produces the <see cref="IOutboundEnvelope{TMessage}" /> using the <see cref="IProduceStrategy" />
    ///     configured in the endpoint.
    /// </summary>
    public class ProduceBehavior : IBehavior, ISorted
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly ConcurrentDictionary<ProducerConfiguration, IProduceStrategyImplementation> _produceStrategyImplementations = new();

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProduceBehavior" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to build the
        ///     <see cref="IProduceStrategyImplementation" />.
        /// </param>
        public ProduceBehavior(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => IntegrationBehaviorsSortIndexes.OutboundProducer;

        /// <inheritdoc cref="IBehavior.HandleAsync" />
        public async Task<IReadOnlyCollection<object?>> HandleAsync(object message, MessageHandler next)
        {
            Check.NotNull(next, nameof(next));

            if (message is IOutboundEnvelope envelope)
            {
                IProduceStrategyImplementation? strategyImplementation = _produceStrategyImplementations.GetOrAdd(
                    envelope.Endpoint.Configuration,
                    static (endpoint, serviceProvider) => endpoint.Strategy.Build(serviceProvider),
                    _serviceProvider);
                await strategyImplementation.ProduceAsync(envelope).ConfigureAwait(false);
            }

            return await next(message).ConfigureAwait(false);
        }
    }
}
