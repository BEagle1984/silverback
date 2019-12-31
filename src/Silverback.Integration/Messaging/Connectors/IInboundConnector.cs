// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Subscribes to a message broker and forwards the incoming integration messages to the internal bus.
    /// </summary>
    public interface IInboundConnector
    {
        IInboundConnector Bind(
            IConsumerEndpoint endpoint,
            IErrorPolicy errorPolicy = null,
            InboundConnectorSettings settings = null);
    }
}