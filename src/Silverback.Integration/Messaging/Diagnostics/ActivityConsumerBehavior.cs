// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Diagnostics
{
    /// <summary>
    ///     Starts an <see cref="Activity" /> with the tracing information from the message headers.
    /// </summary>
    public class ActivityConsumerBehavior : IConsumerBehavior, ISorted
    {
        /// <inheritdoc />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Activity;

        /// <inheritdoc />
        public async Task Handle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(serviceProvider, nameof(serviceProvider));
            Check.NotNull(next, nameof(next));

            var activity = new Activity(DiagnosticsConstants.ActivityNameMessageConsuming);
            try
            {
                TryInitActivity(context, activity, serviceProvider.GetService<ILogger<ActivityConsumerBehavior>>());

                await next(context, serviceProvider);
            }
            finally
            {
                activity.Stop();
            }
        }

        [SuppressMessage("ReSharper", "CA1031", Justification = Justifications.ExceptionLogged)]
        private static void TryInitActivity(ConsumerPipelineContext context, Activity activity, ILogger? logger)
        {
            try
            {
                activity.InitFromMessageHeaders(context.Envelopes.Single().Headers);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to initialize the current activity from the message headers.");
            }

            activity.Start();
        }
    }
}
