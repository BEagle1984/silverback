// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Outbound.Routing;

internal sealed class OutboundRoutingConfiguration : IOutboundRoutingConfiguration
{
    private readonly List<OutboundRoute> _routes = new();

    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<IOutboundRoute>> _routesByMessageType = new();

    public IReadOnlyCollection<IOutboundRoute> Routes => _routes.AsReadOnly();

    public bool PublishOutboundMessagesToInternalBus { get; set; }

    public bool IdempotentEndpointRegistration { get; set; } = true;

    public void AddRoute(Type messageType, ProducerConfiguration producerConfiguration) =>
        _routes.Add(new OutboundRoute(messageType, producerConfiguration));

    public IReadOnlyCollection<IOutboundRoute> GetRoutesForMessage(object message) =>
        GetRoutesForMessage(message.GetType());

    public IReadOnlyCollection<IOutboundRoute> GetRoutesForMessage(Type messageType) =>
        _routesByMessageType.GetOrAdd(
            messageType,
            static (keyMessageType, routes) => GetRoutesForMessage(routes, keyMessageType),
            _routes);

    public bool IsAlreadyRegistered(Type messageType, ProducerConfiguration producerConfiguration)
    {
        ProducerConfiguration? alreadyRegisteredEndpoint = _routes
            .Where(route => route.MessageType == messageType && route.ProducerConfiguration.RawName == producerConfiguration.RawName)
            .Select(route => route.ProducerConfiguration)
            .FirstOrDefault();

        if (alreadyRegisteredEndpoint == null)
            return false;

        if (!producerConfiguration.Equals(alreadyRegisteredEndpoint))
        {
            throw new EndpointConfigurationException(
                $"An endpoint \"{producerConfiguration.RawName}\" for message type \"{messageType.FullName}\" " +
                "is already registered with a different configuration. If this is intentional you can disable " +
                "this check using the AllowDuplicateEndpointRegistrations.");
        }

        return true;
    }

    private static List<OutboundRoute> GetRoutesForMessage(List<OutboundRoute> routes, Type messageType) => routes.Where(route => route.MessageType.IsAssignableFrom(messageType) || IsCompatibleTombstone(route, messageType)).ToList();

    private static bool IsCompatibleTombstone(OutboundRoute route, Type messageType) =>
        typeof(Tombstone).IsAssignableFrom(messageType) &&
        messageType.GenericTypeArguments.Length == 1 &&
        route.MessageType.IsAssignableFrom(messageType.GenericTypeArguments[0]);
}
