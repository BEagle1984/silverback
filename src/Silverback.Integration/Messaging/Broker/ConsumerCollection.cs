// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

internal class ConsumerCollection : IConsumerCollection, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, IConsumer> _consumers = [];

    public int Count => _consumers.Count;

    public IConsumer this[string name] => _consumers[name];

    public void Add(IConsumer consumer)
    {
        foreach (string? friendlyName in consumer.EndpointsConfiguration.Select(configuration => configuration.FriendlyName))
        {
            if (!string.IsNullOrEmpty(friendlyName) && _consumers.Values.Any(
                existingConsumer => existingConsumer.EndpointsConfiguration
                    .Any(configuration => configuration.FriendlyName == friendlyName)))
            {
                throw new InvalidOperationException($"A consumer endpoint with the name '{friendlyName}' has already been added.");
            }
        }

        if (!_consumers.TryAdd(consumer.Name, consumer))
            throw new InvalidOperationException($"A consumer with name '{consumer.Name}' has already been added.");
    }

    public ValueTask StopAllAsync() => _consumers.Values.ForEachAsync(consumer => consumer.StopAsync());

    public IEnumerator<IConsumer> GetEnumerator() => _consumers.Values.GetEnumerator();

    public ValueTask DisposeAsync() => this.DisposeAllAsync();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
