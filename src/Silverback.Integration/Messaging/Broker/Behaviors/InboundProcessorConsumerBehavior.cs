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

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     Handles the retry policies, batch consuming and scope management of the messages that are consumed
    ///     via an inbound connector.
    /// </summary>
    public class InboundProcessorConsumerBehavior : IConsumerBehavior, ISorted
    {
        private readonly ErrorPolicyHelper _errorPolicyHelper;

        public InboundProcessorConsumerBehavior(ErrorPolicyHelper errorPolicyHelper)
        {
            _errorPolicyHelper = errorPolicyHelper ?? throw new ArgumentNullException(nameof(errorPolicyHelper));
        }

        public MessageBatch Batch { get; set; }
        public IErrorPolicy ErrorPolicy { get; set; }

        public async Task Handle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next) =>
            await new InboundProcessor(Batch, ErrorPolicy, _errorPolicyHelper, context, next)
                .ProcessEnvelopes();

        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.InboundProcessor;

        private class InboundProcessor
        {
            private readonly MessageBatch _batch;
            private readonly IErrorPolicy _errorPolicy;
            private readonly ErrorPolicyHelper _errorPolicyHelper;
            private readonly ConsumerPipelineContext _context;
            private readonly ConsumerBehaviorHandler _next;

            public InboundProcessor(
                MessageBatch batch,
                IErrorPolicy errorPolicy,
                ErrorPolicyHelper errorPolicyHelper,
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
                    await ProcessMessagesInBatch();
                else
                    await ProcessMessagesDirectly();
            }

            private async Task ProcessMessagesInBatch()
            {
                _batch.BindOnce(
                    ForwardMessages,
                    Commit,
                    Rollback);

                await _batch.AddMessages(_context.Envelopes);
            }

            private async Task ProcessMessagesDirectly()
            {
                await _errorPolicyHelper.TryProcessAsync(
                    _context,
                    _errorPolicy,
                    ForwardMessages,
                    Commit,
                    Rollback);
            }

            private Task ForwardMessages(ConsumerPipelineContext context, IServiceProvider serviceProvider) =>
                _next(context, serviceProvider);

            private async Task Commit(ConsumerPipelineContext context, IServiceProvider serviceProvider)
            {
                await serviceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new ConsumingCompletedEvent(context.Envelopes));

                if (context.CommitOffsets.Any())
                    await _context.Consumer.Commit(context.CommitOffsets);
            }

            private async Task Rollback(
                ConsumerPipelineContext context,
                IServiceProvider serviceProvider,
                Exception exception)
            {
                await serviceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new ConsumingAbortedEvent(context.Envelopes, exception));

                if (context.CommitOffsets.Any())
                    await _context.Consumer.Rollback(context.CommitOffsets);
            }
        }
    }
}