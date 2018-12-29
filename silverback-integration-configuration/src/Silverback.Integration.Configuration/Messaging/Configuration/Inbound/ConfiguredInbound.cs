// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Configuration.Inbound
{
    public class ConfiguredInbound
    {
        public ConfiguredInbound(Type connectorType, IEndpoint endpoint, IEnumerable<ErrorPolicyBase> errorPolicies)
        {
            ConnectorType = connectorType;
            Endpoint = endpoint;
            ErrorPolicies = errorPolicies.ToArray();
        }

        public Type ConnectorType { get; }
        public IEndpoint Endpoint { get; }
        public ErrorPolicyBase[] ErrorPolicies { get; }
    }
}