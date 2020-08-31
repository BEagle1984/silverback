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
    ///     Handles the retry policies, batch consuming and scope management of the messages that are consumed
    ///     via an inbound connector.
    /// </summary>
    public class InboundProcessorConsumerBehavior : IConsumerBehavior
    {
        private readonly IErrorPolicyHelper _errorPolicyHelper;

        /// <summary>
        ///     Initializes a new instance of the <see cref="InboundProcessorConsumerBehavior" /> class.
        /// </summary>
        /// <param name="errorPolicyHelper">
        ///     The <see cref="IErrorPolicyHelper" /> to be used to apply the defined error policies.
        /// </param>
        public InboundProcessorConsumerBehavior(IErrorPolicyHelper errorPolicyHelper)
        {
            _errorPolicyHelper = Check.NotNull(errorPolicyHelper, nameof(errorPolicyHelper));
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.InboundProcessor;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next) =>
            await new InboundProcessor(_errorPolicyHelper, context, next)
                .ProcessMessages()
                .ConfigureAwait(false);

        private class InboundProcessor
        {
            private readonly ConsumerPipelineContext _context;

            private readonly IErrorPolicyHelper _errorPolicyHelper;

            private readonly ConsumerBehaviorHandler _next;

            public InboundProcessor(
                IErrorPolicyHelper errorPolicyHelper,
                ConsumerPipelineContext context,
                ConsumerBehaviorHandler next)
            {
                _errorPolicyHelper = errorPolicyHelper;
                _context = context;
                _next = next;
            }

            public Task ProcessMessages() =>
                _errorPolicyHelper.TryProcessAsync(
                        _context,
                        ForwardMessages,
                        Commit,
                        Rollback)
                    .ConfigureAwait(false);

            private Task ForwardMessages(ConsumerPipelineContext context, IServiceProvider serviceProvider) =>
                _next(context, serviceProvider);

            private async Task Commit(ConsumerPipelineContext context, IServiceProvider serviceProvider)
            {
                await serviceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new ConsumingCompletedEvent(context))
                    .ConfigureAwait(false);

                if (context.CommitOffsets != null && context.CommitOffsets.Count > 0)
                    await _context.Consumer.Commit((List<IOffset>)context.CommitOffsets).ConfigureAwait(false);
            }

            private async Task Rollback(
                ConsumerPipelineContext context,
                IServiceProvider serviceProvider,
                Exception exception)
            {
                await serviceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new ConsumingAbortedEvent(context, exception))
                    .ConfigureAwait(false);

                if (context.CommitOffsets != null && context.CommitOffsets.Count > 0)
                    await _context.Consumer.Rollback((List<IOffset>)context.CommitOffsets).ConfigureAwait(false);
            }
        }
    }
}
