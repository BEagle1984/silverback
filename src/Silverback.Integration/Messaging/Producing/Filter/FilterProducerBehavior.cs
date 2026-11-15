// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Producing.Filter;

/// <summary>
///     Filter the messages to be produced, based on the configured <see cref="IOutboundMessageFilter" />.
/// </summary>
public class FilterProducerBehavior : IProducerBehavior
{
    private readonly ISilverbackLogger<IProducer> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FilterProducerBehavior" /> class.
    /// </summary>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger{TCategoryName}" />.
    /// </param>
    public FilterProducerBehavior(ISilverbackLogger<IProducer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Filter;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        IOutboundMessageFilter? filter = context.Producer.EndpointConfiguration.Filter;

        if (filter is ServiceProviderOutboundMessageFilter serviceProviderFilter)
            filter = serviceProviderFilter.Resolve(context.ServiceProvider);

        if (filter != null && !await filter.ShouldProduceAsync(context.Envelope, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogOutboundMessageFiltered(context.Envelope);
            return;
        }

        await next(context, cancellationToken).ConfigureAwait(false);
    }
}
