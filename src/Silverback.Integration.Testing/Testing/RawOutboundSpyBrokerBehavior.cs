// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Testing;

/// <summary>
///     Added at the very end of the producer pipeline, forwards the produced <see cref="IOutboundEnvelope" />
///     to the <see cref="IIntegrationSpy" />.
/// </summary>
public class RawOutboundSpyBrokerBehavior : IProducerBehavior
{
    private readonly IntegrationSpy _integrationSpy;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RawOutboundSpyBrokerBehavior" /> class.
    /// </summary>
    /// <param name="integrationSpy">
    ///     The <see cref="IntegrationSpy" />.
    /// </param>
    public RawOutboundSpyBrokerBehavior(IntegrationSpy integrationSpy)
    {
        _integrationSpy = Check.NotNull(integrationSpy, nameof(integrationSpy));
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => int.MaxValue;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        _integrationSpy.AddRawOutboundEnvelope(context.Envelope);

        return next(context, cancellationToken);
    }
}
