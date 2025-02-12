// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Testing;

/// <summary>
///     Subscribes to all the <see cref="IOutboundEnvelope" /> being published via the mediator and forwards
///     them to the <see cref="IIntegrationSpy" /> to be collected.
/// </summary>
/// <remarks>
///     This is used alternatively to the <see cref="InboundSpyBrokerBehavior" />.
/// </remarks>
public class InboundSpySubscriber
{
    private readonly IntegrationSpy _integrationSpy;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InboundSpySubscriber" /> class.
    /// </summary>
    /// <param name="integrationSpy">
    ///     The <see cref="IntegrationSpy" />.
    /// </param>
    public InboundSpySubscriber(IntegrationSpy integrationSpy)
    {
        _integrationSpy = Check.NotNull(integrationSpy, nameof(integrationSpy));
    }

    [Subscribe]
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Called by Silverback")]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by Silverback")]
    private void OnInbound(IInboundEnvelope envelope) => _integrationSpy.AddInboundEnvelope(envelope);
}
