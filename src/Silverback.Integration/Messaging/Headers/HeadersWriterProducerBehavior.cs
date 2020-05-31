// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Headers
{
    /// <summary>
    ///     Maps the properties decorated with the <see cref="HeaderAttribute" /> to the message headers.
    /// </summary>
    public class HeadersWriterProducerBehavior : IProducerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.HeadersWriter;

        /// <inheritdoc cref="IProducerBehavior.Handle" />
        public async Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            HeaderAttributeHelper.GetHeaders(context.Envelope.Message)
                .ForEach(header => context.Envelope.Headers.AddOrReplace(header.Name, header.Value));

            await next(context);
        }
    }
}
