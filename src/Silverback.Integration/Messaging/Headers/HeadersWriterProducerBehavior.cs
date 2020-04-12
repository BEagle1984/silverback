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
    public class HeadersWriterProducerBehavior : IProducerBehavior, ISorted
    {
        public async Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            HeaderAttributeHelper.GetHeaders(context.Envelope.Message)
                .ForEach(header => context.Envelope.Headers.AddOrReplace(header.Key, header.Value));

            await next(context);
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.HeadersWriter;
    }
}