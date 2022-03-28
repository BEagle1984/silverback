// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Producing.Routing;

internal sealed class OutboundRoutingConfiguration : IOutboundRoutingConfiguration
{
    public bool PublishOutboundMessagesToInternalBus { get; set; }
}
