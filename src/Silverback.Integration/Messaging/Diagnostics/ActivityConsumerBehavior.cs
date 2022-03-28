// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Diagnostics;

/// <summary>
///     Starts an <see cref="Activity" /> with the tracing information from the message headers.
/// </summary>
public class ActivityConsumerBehavior : IConsumerBehavior
{
    private readonly IActivityEnricherFactory _activityEnricherFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ActivityConsumerBehavior" /> class.
    /// </summary>
    /// <param name="activityEnricherFactory">
    ///     The <see cref="IActivityEnricherFactory" /> to resolve the
    ///     ActivityEnricher.
    /// </param>
    public ActivityConsumerBehavior(IActivityEnricherFactory activityEnricherFactory)
    {
        _activityEnricherFactory = activityEnricherFactory;
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Activity;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        using Activity activity = ActivitySources.StartConsumeActivity(context.Envelope);

        _activityEnricherFactory.GetActivityEnricher(context.Envelope.Endpoint.Configuration).EnrichInboundActivity(activity, context);

        await next(context).ConfigureAwait(false);
    }
}
