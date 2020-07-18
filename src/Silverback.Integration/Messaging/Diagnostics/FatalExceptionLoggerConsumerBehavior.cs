// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Diagnostics
{
    /// <summary>
    ///     Logs the unhandled exceptions thrown while processing the message. These exceptions are fatal since
    ///     they will usually cause the consumer to stop.
    /// </summary>
    public class FatalExceptionLoggerConsumerBehavior : IConsumerBehavior
    {
        private readonly ISilverbackLogger<FatalExceptionLoggerConsumerBehavior> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FatalExceptionLoggerConsumerBehavior" /> class.
        /// </summary>
        /// <param name="logger">The <see cref="ISilverbackLogger" />.</param>
        public FatalExceptionLoggerConsumerBehavior(ISilverbackLogger<FatalExceptionLoggerConsumerBehavior> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.FatalExceptionLogger;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(serviceProvider, nameof(serviceProvider));
            Check.NotNull(next, nameof(next));

            try
            {
                await next(context, serviceProvider).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogCriticalWithMessageInfo(
                    EventIds.ConsumerFatalError,
                    ex,
                    "Fatal error occurred consuming the message. The consumer will be stopped.",
                    context.Envelopes);

                throw;
            }
        }
    }
}
