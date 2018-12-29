// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration.Outbound
{
    public class ConfiguredOutbound
    {
        public ConfiguredOutbound(Type messageType, Type connectorType, IEndpoint endpoint)
        {
            MessageType = messageType;
            ConnectorType = connectorType;
            Endpoint = endpoint;
        }

        public Type MessageType { get; }
        public Type ConnectorType { get; }
        public IEndpoint Endpoint { get; }
    }
}