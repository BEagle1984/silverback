// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Diagnostics;

/// <summary>
///     Logs the unhandled exceptions thrown while processing the message. These exceptions are fatal since
///     they will usually cause the consumer to stop.
/// </summary>
public class FatalExceptionLoggerConsumerBehavior : IConsumerBehavior
{
    private readonly IInboundLogger<FatalExceptionLoggerConsumerBehavior> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FatalExceptionLoggerConsumerBehavior" /> class.
    /// </summary>
    /// <param name="logger">
    ///     The <see cref="IInboundLogger{TCategoryName}" />.
    /// </param>
    public FatalExceptionLoggerConsumerBehavior(IInboundLogger<FatalExceptionLoggerConsumerBehavior> logger)
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
            _logger.LogProcessingFatalError(context.Envelope, ex);

            throw new ConsumerPipelineFatalException(
                "Fatal error occurred processing the consumed message.",
                ex);
        }
    }
}
