// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
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
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            IServiceProvider serviceProvider,
            IConsumer consumer,
            RawInboundEnvelopeHandler next) =>
            await new InboundProcessor(Batch, ErrorPolicy, _errorPolicyHelper, serviceProvider, consumer, next)
                .Handle(envelopes);

        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.InboundProcessor;

        private class InboundProcessor
        {
            private readonly MessageBatch _batch;
            private readonly IErrorPolicy _errorPolicy;
            private readonly ErrorPolicyHelper _errorPolicyHelper;
            private readonly IServiceProvider _serviceProvider;
            private readonly IConsumer _consumer;
            private readonly RawInboundEnvelopeHandler _next;

            public InboundProcessor(
                MessageBatch batch,
                IErrorPolicy errorPolicy,
                ErrorPolicyHelper errorPolicyHelper,
                IServiceProvider serviceProvider,
                IConsumer consumer,
                RawInboundEnvelopeHandler next)
            {
                _batch = batch;
                _errorPolicy = errorPolicy;
                _errorPolicyHelper = errorPolicyHelper;
                _serviceProvider = serviceProvider;
                _consumer = consumer;
                _next = next;
            }

            public async Task Handle(IReadOnlyCollection<IRawInboundEnvelope> envelopes)
            {
                if (_batch != null)
                    await ProcessMessagesInBatch(envelopes);
                else
                    await _errorPolicyHelper.TryProcessAsync(envelopes, _errorPolicy, ProcessMessages);
            }

            private async Task ProcessMessagesInBatch(IReadOnlyCollection<IRawInboundEnvelope> envelopes)
            {
                _batch.BindOnce(
                    (envelopeCollection, batchServiceProvider) =>
                        ForwardMessages(envelopeCollection, batchServiceProvider),
                    Commit,
                    Rollback);

                await _batch.AddMessages(envelopes);
            }

            private async Task ProcessMessages(IReadOnlyCollection<IRawInboundEnvelope> envelopes)
            {
                using var scope = _serviceProvider.CreateScope();

                try
                {
                    await ForwardMessages(envelopes, scope.ServiceProvider);
                    await Commit(envelopes, scope.ServiceProvider);
                }
                catch (Exception)
                {
                    await Rollback(envelopes, scope.ServiceProvider);
                    throw;
                }
            }

            // Called by the MessageBatch as well (therefore must get the IServiceProvider as parameter)
            private Task ForwardMessages(
                IReadOnlyCollection<IRawInboundEnvelope> envelopes,
                IServiceProvider serviceProvider) =>
                _next(envelopes, serviceProvider, _consumer);

            // Called by the MessageBatch as well (therefore must get the IServiceProvider as parameter)
            private async Task Commit(
                IReadOnlyCollection<IRawInboundEnvelope> envelopes,
                IServiceProvider serviceProvider)
            {
                var offsets = envelopes.Select(envelope => envelope.Offset).ToList();

                await serviceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new ConsumingCompletedEvent(envelopes));
                await _consumer.Commit(offsets);
            }

            // Called by the MessageBatch as well (therefore must get the IServiceProvider as parameter)
            private async Task Rollback(
                IReadOnlyCollection<IRawInboundEnvelope> envelopes,
                IServiceProvider serviceProvider)
            {
                var offsets = envelopes.Select(envelope => envelope.Offset).ToList();

                await _consumer.Rollback(offsets);
                await serviceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(new ConsumingAbortedEvent(envelopes));
            }
        }
    }
}