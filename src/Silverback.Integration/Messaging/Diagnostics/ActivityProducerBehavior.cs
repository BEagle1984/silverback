﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Diagnostics;

/// <summary>
///     Starts an <see cref="Activity" /> and adds the tracing information to the message headers.
/// </summary>
public class ActivityProducerBehavior : IProducerBehavior
{
    private readonly IActivityEnricherFactory _activityEnricherFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ActivityProducerBehavior" /> class.
    /// </summary>
    /// <param name="activityEnricherFactory">
    ///     The Factory to create the activity enrichers.
    /// </param>
    public ActivityProducerBehavior(IActivityEnricherFactory activityEnricherFactory)
    {
        _activityEnricherFactory = activityEnricherFactory;
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Activity;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public async Task HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        using Activity activity = ActivitySources.StartProduceActivity(context.Envelope);
        _activityEnricherFactory
            .GetActivityEnricher(context.Envelope.Endpoint.Configuration)
            .EnrichOutboundActivity(activity, context);
        await next(context).ConfigureAwait(false);
    }
}
