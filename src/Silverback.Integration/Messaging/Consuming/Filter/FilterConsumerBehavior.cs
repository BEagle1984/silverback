// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.Filter;

/// <summary>
///     Filters the consumed messages, based on the configured <see cref="IInboundMessageFilter" />.
/// </summary>
/// <remarks>
///     The filtered messages aren't delivered to any subscriber, but they still go through the rest of the pipeline, to be committed (or
///     acknowledged) like the messages that no subscriber handles. In a sequence (e.g. a batch), they don't count toward its length.
/// </remarks>
public class FilterConsumerBehavior : IConsumerBehavior
{
    private readonly ISilverbackLogger<FilterConsumerBehavior> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FilterConsumerBehavior" /> class.
    /// </summary>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger{TCategoryName}" />.
    /// </param>
    public FilterConsumerBehavior(ISilverbackLogger<FilterConsumerBehavior> logger)
    {
        _logger = Check.NotNull(logger);
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Filter;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context);
        Check.NotNull(next);

        IInboundMessageFilter? filter = context.Envelope.Endpoint.Configuration.Filter;

        if (filter != null)
            await ApplyFilterAsync(filter, context, cancellationToken).ConfigureAwait(false);

        // Call the next behaviors also for the filtered message, which they skip (see PublisherConsumerBehavior and SequenceBase), so
        // that it's committed (or acknowledged) like a message that no subscriber handles. Returning here would commit it right away
        // instead, also while a pending sequence (e.g. a batch) still holds the previous messages, and would skip the abort of the
        // pending sequences.
        await next(context, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask ApplyFilterAsync(
        IInboundMessageFilter filter,
        ConsumerPipelineContext context,
        CancellationToken cancellationToken)
    {
        if (filter is ServiceProviderInboundMessageFilter serviceProviderFilter)
            filter = serviceProviderFilter.Resolve(context.ServiceProvider);

        // The deserializer wraps each message in a new InboundEnvelope, which carries the outcome to the next behaviors (see
        // PublisherConsumerBehavior and SequenceBase)
        if (context.Envelope is not InboundEnvelope envelope)
            throw new InvalidOperationException("The message must be deserialized before being filtered.");

        envelope.IsFiltered = !await filter.ShouldProcessAsync(envelope, cancellationToken).ConfigureAwait(false);

        if (envelope.IsFiltered)
            _logger.LogInboundMessageFiltered(envelope);
    }
}
