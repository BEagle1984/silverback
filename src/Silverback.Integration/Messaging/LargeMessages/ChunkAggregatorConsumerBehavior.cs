// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
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
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            IServiceProvider serviceProvider,
            IConsumer consumer,
            RawInboundEnvelopeHandler next)
        {
            var newEnvelopes = (await envelopes.SelectAsync(envelope => AggregateIfNeeded(envelope, serviceProvider)))
                .Where(envelope => envelope != null).ToList();

            if (newEnvelopes.Any())
                await next(newEnvelopes, serviceProvider, consumer);
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.ChunkAggregator;

        private async Task<IRawInboundEnvelope> AggregateIfNeeded(
            IRawInboundEnvelope envelope,
            IServiceProvider serviceProvider)
        {
            if (!envelope.Headers.Contains(DefaultMessageHeaders.ChunkId))
                return envelope;

            var completeMessage =
                await serviceProvider.GetRequiredService<ChunkAggregator>().AggregateIfComplete(envelope);

            return completeMessage == null
                ? null
                : new RawInboundEnvelope(
                    completeMessage,
                    envelope.Headers,
                    envelope.Endpoint,
                    envelope.ActualEndpointName,
                    envelope.Offset);
        }
    }
}