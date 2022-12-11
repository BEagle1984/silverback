// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Producing.Routing;

/// <summary>
///     Holds the outbound messages routing configuration (which message is redirected to which endpoint).
/// </summary>
public interface IOutboundRoutingConfiguration
{
    /// <summary>
    ///     Gets or sets a value indicating whether the messages to be routed through an outbound connector have
    ///     also to be published to the internal bus, to be locally subscribed. The default is <c>false</c>.
    /// </summary>
    // TODO: Move in producer endpoint configuration  and delete this interface
    bool PublishOutboundMessagesToInternalBus { get; set; }
}
