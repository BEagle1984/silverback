// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// Holds the outbound messages routing configuration (which message is redirected to which endpoint).
    /// </summary>
    public interface IOutboundRoutingConfiguration
    {
        IEnumerable<IOutboundRoute> Routes { get; }

        IOutboundRoutingConfiguration Add<TMessage>(IEndpoint endpoint, Type outboundConnectorType = null);

        IOutboundRoutingConfiguration Add(Type messageType, IEndpoint endpoint, Type outboundConnectorType = null);

        IEnumerable<IOutboundRoute> GetRoutesForMessage(object message);
    }
}