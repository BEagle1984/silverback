// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Diagnostics
{
    /// <summary>
    ///     Starts an <see cref="Activity" /> with the tracing information from the message headers.
    /// </summary>
    public class ActivityConsumerBehavior : IConsumerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Activity;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            var activity = new Activity(DiagnosticsConstants.ActivityNameMessageConsuming);
            try
            {
                TryInitActivity(
                    context,
                    activity,
                    context.ServiceProvider.GetService<ISilverbackLogger<ActivityConsumerBehavior>>());

                await next(context).ConfigureAwait(false);
            }
            finally
            {
                activity.Stop();
            }
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private static void TryInitActivity(
            ConsumerPipelineContext context,
            Activity activity,
            ISilverbackLogger? logger)
        {
            try
            {
                activity.InitFromMessageHeaders(context.Envelope.Headers);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(
                    IntegrationEventIds.ErrorInitializingActivity,
                    ex,
                    "Failed to initialize the current activity from the message headers.");
            }

            activity.Start();
        }
    }
}
