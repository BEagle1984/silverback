// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Headers
{
    /// <summary>
    ///     Maps the headers with the properties decorated with the <see cref="HeaderAttribute" />.
    /// </summary>
    public class HeadersReaderConsumerBehavior : IConsumerBehavior, ISorted
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.HeadersReader;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(serviceProvider, nameof(serviceProvider));
            Check.NotNull(next, nameof(next));

            context.Envelopes.OfType<InboundEnvelope>()
                .ForEach(
                    envelope => HeaderAttributeHelper.SetFromHeaders(
                        envelope.Message,
                        envelope.Headers));

            await next(context, serviceProvider);
        }
    }
}
