﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.ExactlyOnce;

/// <summary>
///     Uses the configured implementation of <see cref="IExactlyOnceStrategy" /> to ensure that the message
///     is processed only once.
/// </summary>
public class ExactlyOnceGuardConsumerBehavior : IConsumerBehavior
{
    private readonly IInboundLogger<ExactlyOnceGuardConsumerBehavior> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExactlyOnceGuardConsumerBehavior" /> class.
    /// </summary>
    /// <param name="logger">
    ///     The <see cref="IInboundLogger{TCategoryName}" />.
    /// </param>
    public ExactlyOnceGuardConsumerBehavior(IInboundLogger<ExactlyOnceGuardConsumerBehavior> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.ExactlyOnceGuard;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public async Task HandleAsync(
        ConsumerPipelineContext context,
        ConsumerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (!await CheckIsAlreadyProcessedAsync(context).ConfigureAwait(false))
            await next(context).ConfigureAwait(false);
    }

    private async Task<bool> CheckIsAlreadyProcessedAsync(ConsumerPipelineContext context)
    {
        if (context.Envelope.Endpoint.Configuration.ExactlyOnceStrategy == null)
            return false;

        IExactlyOnceStrategyImplementation strategyImplementation = context.Envelope.Endpoint.Configuration.ExactlyOnceStrategy.Build(context.ServiceProvider);

        if (!await strategyImplementation.CheckIsAlreadyProcessedAsync(context).ConfigureAwait(false))
            return false;

        _logger.LogAlreadyProcessed(context.Envelope);

        return true;
    }
}
