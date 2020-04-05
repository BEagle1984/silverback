// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.LargeMessages
{
    /// <summary>
    ///     Temporary stores and aggregates the message chunks to rebuild the original message.
    /// </summary>
    public class ChunkAggregatorConsumerBehavior : IConsumerBehavior, ISorted
    {
        public async Task Handle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next)
        {
            context.Envelopes = (await context.Envelopes.SelectAsync(envelope =>
                    AggregateIfNeeded(envelope, serviceProvider)))
                .Where(envelope => envelope != null).ToList();

            if (context.Envelopes.Any())
                await next(context, serviceProvider);
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.ChunkAggregator;

        private async Task<IRawInboundEnvelope> AggregateIfNeeded(
            IRawInboundEnvelope envelope,
            IServiceProvider serviceProvider)
        {
            if (!envelope.Headers.Contains(DefaultMessageHeaders.ChunkId) ||
                envelope.Headers.Contains(DefaultMessageHeaders.ChunkAggregated))
            {
                return envelope;
            }

            var completeMessage =
                await serviceProvider.GetRequiredService<ChunkAggregator>().AggregateIfComplete(envelope);

            if (completeMessage == null)
                return null;

            var completeMessageEnvelope = new RawInboundEnvelope(
                completeMessage,
                envelope.Headers,
                envelope.Endpoint,
                envelope.ActualEndpointName,
                envelope.Offset);

            completeMessageEnvelope.Headers.Add(DefaultMessageHeaders.ChunkAggregated, "true");

            return completeMessageEnvelope;
        }
    }
}