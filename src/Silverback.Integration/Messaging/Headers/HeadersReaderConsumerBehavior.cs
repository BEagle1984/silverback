// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Headers;

/// <summary>
///     Maps the headers with the properties decorated with the <see cref="HeaderAttribute" />.
/// </summary>
public class HeadersReaderConsumerBehavior : IConsumerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.HeadersReader;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public async Task HandleAsync(
        ConsumerPipelineContext context,
        ConsumerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Envelope is IInboundEnvelope inboundEnvelope)
        {
            HeaderAttributeHelper.SetFromHeaders(
                inboundEnvelope.Message,
                inboundEnvelope.Headers);
        }

        await next(context).ConfigureAwait(false);
    }
}
