// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
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

        internal MessageBatch? Batch { get; set; }

        internal IErrorPolicy? ErrorPolicy { get; set; }

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next) =>
            await new InboundProcessor(Batch!, ErrorPolicy!, _errorPolicyHelper, context, next)
                .ProcessEnvelopes()
                .ConfigureAwait(false);

        private class InboundProcessor
        {
            private readonly MessageBatch _batch;

            private readonly ConsumerPipelineContext _context;

            private readonly IErrorPolicy _errorPolicy;

            private readonly IErrorPolicyHelper _errorPolicyHelper;

            private readonly ConsumerBehaviorHandler _next;

            public InboundProcessor(
                MessageBatch batch,
                IErrorPolicy errorPolicy,
                IErrorPolicyHelper errorPolicyHelper,
                ConsumerPipelineContext context,
                ConsumerBehaviorHandler next)
            {
                _batch = batch;
                _errorPolicy = errorPolicy;
                _errorPolicyHelper = errorPolicyHelper;
                _context = context;
                _next = next;
            }

            public async Task ProcessEnvelopes()
            {
                if (_batch != null)
                    await ProcessMessagesInBatch().ConfigureAwait(false);
                else
                    await ProcessMessagesDirectly().ConfigureAwait(false);
            }

            private async Task ProcessMessagesInBatch()
            {
                _batch.BindOnce(
                    ForwardMessages,
                    Commit,
                    Rollback);

                await _batch.AddMessages(_context.Envelopes).ConfigureAwait(false);
            }

            private async Task ProcessMessagesDirectly()
            {
                await _errorPolicyHelper.TryProcessAsync(
                    _context,
                    _errorPolicy,
                    ForwardMessages,
                    Commit,
                    Rollback)
                    .ConfigureAwait(false);
            }

            private Task ForwardMessages(ConsumerPipelineContext context, IServiceProvider serviceProvider) =>
                _next(context, serviceProvider);

            private async Task Commit(ConsumerPipelineContext context, IServiceProvider serviceProvider)
            {
                await serviceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new ConsumingCompletedEvent(context))
                    .ConfigureAwait(false);

                if (context.CommitOffsets != null && context.CommitOffsets.Any())
                    await _context.Consumer.Commit(context.CommitOffsets).ConfigureAwait(false);
            }

            private async Task Rollback(
                ConsumerPipelineContext context,
                IServiceProvider serviceProvider,
                Exception exception)
            {
                await serviceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new ConsumingAbortedEvent(context, exception))
                    .ConfigureAwait(false);

                if (context.CommitOffsets != null && context.CommitOffsets.Any())
                    await _context.Consumer.Rollback(context.CommitOffsets).ConfigureAwait(false);
            }
        }
    }
}
