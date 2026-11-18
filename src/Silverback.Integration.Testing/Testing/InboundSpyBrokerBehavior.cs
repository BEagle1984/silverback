// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Testing;

/// <summary>
///     Added at the end of the consumer pipeline, forwards the processed
///     <see cref="IInboundEnvelope" /> to the <see cref="IIntegrationSpy" />.
/// </summary>
public class InboundSpyBrokerBehavior : IConsumerBehavior
{
    private readonly IntegrationSpy _integrationSpy;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InboundSpyBrokerBehavior" /> class.
    /// </summary>
    /// <param name="integrationSpy">
    ///     The <see cref="IntegrationSpy" />.
    /// </param>
    public InboundSpyBrokerBehavior(IntegrationSpy integrationSpy)
    {
        _integrationSpy = Check.NotNull(integrationSpy);
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Publisher - 1;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context);
        Check.NotNull(next);

        // Skip the messages rejected by the endpoint filter, which aren't delivered to the subscribers, like the subscriber-based spy does
        // (see InboundSpySubscriber) and like the outbound spy, which doesn't get the filtered messages either
        if (context.Envelope is IInboundEnvelope inboundEnvelope and not InboundEnvelope { IsFiltered: true })
            _integrationSpy.AddInboundEnvelope(inboundEnvelope);

        return next(context, cancellationToken);
    }
}
