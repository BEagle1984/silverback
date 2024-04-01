// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Silverback.Messaging.Configuration.Mqtt;

// TODO: Test
internal class MqttConsumerEndpointsCache
{
    private readonly ConcurrentDictionary<string, MqttConsumerEndpoint> _endpoints = new();

    private readonly List<WildcardSubscription> _wildcardSubscriptions = [];

    public MqttConsumerEndpointsCache(MqttClientConfiguration configuration)
    {
        foreach (MqttConsumerEndpointConfiguration endpointConfiguration in configuration.ConsumerEndpoints)
        {
            foreach (string topic in endpointConfiguration.Topics)
            {
                ParsedTopic parsedTopic = new(topic);

                if (parsedTopic.Regex != null)
                {
                    _wildcardSubscriptions.Add(new WildcardSubscription(parsedTopic.Regex, endpointConfiguration));
                }
                else
                {
                    _endpoints.TryAdd(parsedTopic.Topic, new MqttConsumerEndpoint(parsedTopic.Topic, endpointConfiguration));
                }
            }
        }
    }

    public MqttConsumerEndpoint GetEndpoint(string topic)
    {
        if (_endpoints.TryGetValue(topic, out MqttConsumerEndpoint? endpoint))
            return endpoint;

        WildcardSubscription subscription = _wildcardSubscriptions.Find(subscription => subscription.Regex.IsMatch(topic)) ??
                                            throw new InvalidOperationException($"No configuration found for the specified topic '{topic}'.");

        endpoint = new MqttConsumerEndpoint(topic, subscription.EndpointConfiguration);
        _endpoints.TryAdd(topic, endpoint);
        return endpoint;
    }

    private sealed record WildcardSubscription(Regex Regex, MqttConsumerEndpointConfiguration EndpointConfiguration);
}
