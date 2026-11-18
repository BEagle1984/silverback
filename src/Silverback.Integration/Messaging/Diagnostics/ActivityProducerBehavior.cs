// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Diagnostics;

/// <summary>
///     Starts an <see cref="Activity" /> and adds the tracing information to the message headers.
/// </summary>
/// <remarks>
///     The activity is enriched by the producer, when the message has been produced and right before logging it.
/// </remarks>
public class ActivityProducerBehavior : IProducerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Activity;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        Activity activity = ActivitySources.StartProduceActivity(context.Envelope);
        context.Activity = activity;

        // When producing with callbacks, the activity is stopped when the messages are acknowledged. It stays current only until this
        // method returns anyway, since the changes an async method makes to the execution context don't flow back to its caller.
        bool isStopDeferred = context.TryDeferActivityStop(activity);

        try
        {
            await next(context, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // The callbacks are never invoked when the pipeline throws, so the activity would never be stopped
            if (isStopDeferred)
                activity.Stop();

            throw;
        }
        finally
        {
            if (!isStopDeferred)
                activity.Stop();
        }
    }
}
