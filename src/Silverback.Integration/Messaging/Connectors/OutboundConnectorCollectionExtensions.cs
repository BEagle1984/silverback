// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Connectors
{
    // TODO: Test
    internal static class ConnectorCollectionsExtensions
    {
        public static IOutboundConnector GetConnectorInstance(
            this IEnumerable<IOutboundConnector> connectors,
            Type connectorType) =>
            GetConnectorInstance<IOutboundConnector>(connectors, connectorType);

        public static IInboundConnector GetConnectorInstance(
            this IEnumerable<IInboundConnector> connectors,
            Type connectorType) =>
            GetConnectorInstance<IInboundConnector>(connectors, connectorType);

        private static TConnector GetConnectorInstance<TConnector>(
            this IEnumerable<TConnector> connectors,
            Type connectorType)
        {
            TConnector connector;

            if (connectorType == null)
            {
                connector = connectors.FirstOrDefault();
            }
            else
            {
                connector = connectors.FirstOrDefault(c => c.GetType() == connectorType);

                if (EqualityComparer<TConnector>.Default.Equals(connector, default))
                {
                    connector = connectors.FirstOrDefault(c => connectorType.IsInstanceOfType(c));
                }
            }

            if (connector == null)
                throw new SilverbackException(
                    $"No instance of {connectorType?.Name ?? "IOutboundConnector"} was resolved.");

            return connector;
        }
    }
}