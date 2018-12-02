// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Connectors
{
    public interface IOutboundRoute
    {
        Type MessageType { get; }
        IEndpoint DestinationEndpoint { get; }
        Type OutboundConnectorType { get; }
    }
}