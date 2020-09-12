// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Batch;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     Handles the consumer transaction.
    /// </summary>
    public class TransactionHandlerConsumerBehavior : IConsumerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.TransactionHandler;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
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
                await Rollback(context, ex).ConfigureAwait(false);
                throw;
            }

            // TODO: Should be inside try block?
            await Commit(context).ConfigureAwait(false);
        }

        private async Task Commit(ConsumerPipelineContext context)
        {
            await context.ServiceProvider.GetRequiredService<IPublisher>()
                .PublishAsync(new ConsumingCompletedEvent(context))
                .ConfigureAwait(false);

            if (context.Envelope.Offset != null)
                await context.Consumer.Commit(context.Envelope.Offset).ConfigureAwait(false);
        }

        private async Task Rollback(ConsumerPipelineContext context, Exception exception)
        {
            await context.ServiceProvider.GetRequiredService<IPublisher>()
                .PublishAsync(new ConsumingAbortedEvent(context, exception))
                .ConfigureAwait(false);

            if (context.Envelope.Offset != null)
                await context.Consumer.Rollback(context.Envelope.Offset).ConfigureAwait(false);
        }

        // private class InboundProcessor
        // {
        //     private readonly ConsumerPipelineContext _context;
        //
        //     private readonly IErrorPolicyHelper _errorPolicyHelper;
        //
        //     private readonly ConsumerBehaviorHandler _next;
        //
        //     public InboundProcessor(
        //         IErrorPolicyHelper errorPolicyHelper,
        //         ConsumerPipelineContext context,
        //         ConsumerBehaviorHandler next)
        //     {
        //         _errorPolicyHelper = errorPolicyHelper;
        //         _context = context;
        //         _next = next;
        //     }
        //
        //     public Task ProcessMessages() =>
        //         _errorPolicyHelper.TryProcessAsync(
        //                 _context,
        //                 ForwardMessages,
        //                 Commit,
        //                 Rollback)
        //             .ConfigureAwait(false);
        //
        //     private Task ForwardMessages(ConsumerPipelineContext context, IServiceProvider serviceProvider) =>
        //         _next(context, serviceProvider);
        //
        //     private async Task Commit(ConsumerPipelineContext context, IServiceProvider serviceProvider)
        //     {
        //         await serviceProvider.GetRequiredService<IPublisher>()
        //             .PublishAsync(new ConsumingCompletedEvent(context))
        //             .ConfigureAwait(false);
        //
        //         if (context.CommitOffsets != null && context.CommitOffsets.Count > 0)
        //             await _context.Consumer.Commit((List<IOffset>)context.CommitOffsets).ConfigureAwait(false);
        //     }
        //
        //     private async Task Rollback(
        //         ConsumerPipelineContext context,
        //         IServiceProvider serviceProvider,
        //         Exception exception)
        //     {
        //         await serviceProvider.GetRequiredService<IPublisher>()
        //             .PublishAsync(new ConsumingAbortedEvent(context, exception))
        //             .ConfigureAwait(false);
        //
        //         if (context.CommitOffsets != null && context.CommitOffsets.Count > 0)
        //             await _context.Consumer.Rollback((List<IOffset>)context.CommitOffsets).ConfigureAwait(false);
        //     }
        // }
    }
}
