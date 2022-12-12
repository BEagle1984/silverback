// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Diagnostics;

internal sealed class NullBrokerActivityEnricher : IBrokerActivityEnricher
{
    private NullBrokerActivityEnricher()
    {
    }

    public static NullBrokerActivityEnricher Instance { get; } = new();

    public void EnrichOutboundActivity(Activity activity, ProducerPipelineContext producerContext)
    {
        // Do nothing
    }

    public void EnrichInboundActivity(Activity activity, ConsumerPipelineContext consumerContext)
    {
        // Do nothing
    }
}
