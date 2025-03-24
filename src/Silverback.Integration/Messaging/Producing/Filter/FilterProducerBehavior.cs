// Copyright (c) 2025 Sergio Aquilini
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
    private readonly IProducerLogger<IProducer> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FilterProducerBehavior" /> class.
    /// </summary>
    /// <param name="logger">
    ///     The <see cref="IProducerLogger{TCategoryName}" />.
    /// </param>
    public FilterProducerBehavior(IProducerLogger<IProducer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Filter;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Producer.EndpointConfiguration.Filter != null &&
            !context.Producer.EndpointConfiguration.Filter.ShouldProduce(context.Envelope))
        {
            _logger.LogFiltered(context.Envelope);
            return ValueTask.CompletedTask;
        }

        return next(context, cancellationToken);
    }
}
