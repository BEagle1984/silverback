// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Diagnostics;

internal sealed class NullBrokerActivityEnricher : IBrokerActivityEnricher
{
    private NullBrokerActivityEnricher()
    {
    }

    public static NullBrokerActivityEnricher Instance { get; } = new();

    public void EnrichOutboundActivity(Activity activity, IOutboundEnvelope envelope)
    {
        // Do nothing
    }

    public void EnrichInboundActivity(Activity activity, IRawInboundEnvelope envelope)
    {
        // Do nothing
    }
}
