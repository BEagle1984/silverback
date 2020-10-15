// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Diagnostics
{
    /// <summary>
    ///     Logs the unhandled exceptions thrown while processing the message. These exceptions are fatal since
    ///     they will usually cause the consumer to stop.
    /// </summary>
    public class FatalExceptionLoggerConsumerBehavior : IConsumerBehavior
    {
        private readonly ISilverbackIntegrationLogger<FatalExceptionLoggerConsumerBehavior> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FatalExceptionLoggerConsumerBehavior" /> class.
        /// </summary>
        /// <param name="logger">The <see cref="ISilverbackIntegrationLogger" />.</param>
        public FatalExceptionLoggerConsumerBehavior(ISilverbackIntegrationLogger<FatalExceptionLoggerConsumerBehavior> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.FatalExceptionLogger;

        /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
        public async Task HandleAsync(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogCriticalWithMessageInfo(
                    IntegrationEventIds.ConsumerFatalError,
                    ex,
                    "Fatal error occurred consuming the message. The consumer will be stopped.",
                    context.Envelope);

                throw;
            }
        }
    }
}
