// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// Holds the outbound messages routing configuration (which message is redirected to which endpoint).
    /// </summary>
    public interface IOutboundRoutingConfiguration
    {
        IOutboundRoutingConfiguration Add<TMessage>(IEndpoint endpoint, Type outboundConnectorType = null) where TMessage : IIntegrationMessage;

        IEnumerable<IOutboundRoute> GetRoutes(IIntegrationMessage message);
    }
}