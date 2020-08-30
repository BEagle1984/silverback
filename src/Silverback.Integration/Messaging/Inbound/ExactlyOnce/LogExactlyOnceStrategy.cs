// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Inbound.ExactlyOnce.Repositories;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.ExactlyOnce
{
    /// <summary>
    ///     Uses an <see cref="IInboundLog" /> to keep track of each processed message and guarantee that each
    ///     one is processed only once.
    /// </summary>
    public class LogExactlyOnceStrategy : IExactlyOnceStrategy
    {
        /// <inheritdoc cref="IExactlyOnceStrategy.Build" />
        public IExactlyOnceStrategyImplementation Build(IServiceProvider serviceProvider) =>
            new LogExactlyOnceStrategyImplementation(serviceProvider.GetRequiredService<IInboundLog>());

        private class LogExactlyOnceStrategyImplementation : IExactlyOnceStrategyImplementation
        {
            private readonly IInboundLog _inboundLog;

            public LogExactlyOnceStrategyImplementation(IInboundLog inboundLog)
            {
                _inboundLog = inboundLog;
            }

            public async Task<bool> CheckIsAlreadyProcessedAsync(ConsumerPipelineContext context)
            {
                Check.NotNull(context, nameof(context));

                if (await _inboundLog.ExistsAsync(context.Envelope).ConfigureAwait(false))
                    return true;

                context.TransactionManager.Enlist(_inboundLog);

                await _inboundLog.AddAsync(context.Envelope).ConfigureAwait(false);
                return false;
            }
        }
    }
}
