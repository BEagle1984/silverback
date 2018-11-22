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