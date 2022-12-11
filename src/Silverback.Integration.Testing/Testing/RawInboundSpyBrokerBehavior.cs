// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Testing;

/// <summary>
///     Added at the very beginning of the consumer pipeline, forwards the untouched
///     <see cref="IRawInboundEnvelope" /> to the <see cref="IIntegrationSpy" />.
/// </summary>
public class RawInboundSpyBrokerBehavior : IConsumerBehavior
{
    private readonly IntegrationSpy _integrationSpy;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RawInboundSpyBrokerBehavior" /> class.
    /// </summary>
    /// <param name="integrationSpy">
    ///     The <see cref="IntegrationSpy" />.
    /// </param>
    public RawInboundSpyBrokerBehavior(IntegrationSpy integrationSpy)
    {
        _integrationSpy = Check.NotNull(integrationSpy, nameof(integrationSpy));
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => int.MinValue;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        _integrationSpy.AddRawInboundEnvelope(context.Envelope);

        return next(context);
    }
}
