// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
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
        private readonly IInboundLogger<ActivityConsumerBehavior> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ActivityConsumerBehavior" /> class.
        /// </summary>
        /// <param name="logger">
        ///     The <see cref="IInboundLogger{TCategoryName}" />.
        /// </param>
        public ActivityConsumerBehavior(IInboundLogger<ActivityConsumerBehavior> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Activity;

        /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        public async Task HandleAsync(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            using var activity = new Activity(DiagnosticsConstants.ActivityNameMessageConsuming);

            try
            {
                activity.InitFromMessageHeaders(context.Envelope.Headers);
            }
            catch (Exception ex)
            {
                _logger.LogErrorInitializingActivity(context.Envelope, ex);
            }

            try
            {
                activity.Start();
                await next(context).ConfigureAwait(false);
            }
            finally
            {
                activity.Stop();
            }
        }
    }
}
