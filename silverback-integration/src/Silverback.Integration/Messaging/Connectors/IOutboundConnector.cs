// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// Subscribes to the internal bus and forwards the integration messages to the message broker.
    /// </summary>
    public interface IOutboundConnector
    {
        Task RelayMessage(object message, IEnumerable<MessageHeader> headers, IEndpoint destinationEndpoint);
    }
}