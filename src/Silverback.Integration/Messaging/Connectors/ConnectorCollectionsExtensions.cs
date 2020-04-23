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
            this IReadOnlyCollection<IOutboundConnector> connectors,
            Type? connectorType) =>
            GetConnectorInstance<IOutboundConnector>(connectors, connectorType);

        public static IInboundConnector GetConnectorInstance(
            this IReadOnlyCollection<IInboundConnector> connectors,
            Type? connectorType) =>
            GetConnectorInstance<IInboundConnector>(connectors, connectorType);

        private static TConnector GetConnectorInstance<TConnector>(
            this IReadOnlyCollection<TConnector> connectors,
            Type? connectorType)
            where TConnector : class
        {
            if (connectors == null)
                throw new ArgumentNullException(nameof(connectors));

            if (connectors.Count == 0)
                throw new ArgumentException("The collection is empty.", nameof(connectors));

            if (connectorType == null)
            {
                return connectors.First();
            }

            return connectors.FirstOrDefault(connector => connector.GetType() == connectorType) ??
                   connectors.FirstOrDefault(connector => connectorType.IsInstanceOfType(connector)) ??
                   throw new InvalidOperationException(
                       $"No instance of {connectorType.Name} could be found in the collection of available connectors.");
        }
    }
}
