// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

internal class ConsumerCollection : IConsumerCollection, IAsyncDisposable
{
    private readonly List<IConsumer> _consumers = [];

    public int Count => _consumers.Count;

    public IConsumer this[int index] => _consumers[index];

    public void Add(IConsumer consumer)
    {
        foreach (string? friendlyName in consumer.EndpointsConfiguration.Select(configuration => configuration.FriendlyName))
        {
            if (!string.IsNullOrEmpty(friendlyName) && _consumers.Exists(
                existingConsumer => existingConsumer.EndpointsConfiguration
                    .Any(configuration => configuration.FriendlyName == friendlyName)))
            {
                throw new InvalidOperationException($"A consumer endpoint with the name '{friendlyName}' has already been added.");
            }
        }

        _consumers.Add(consumer);
    }

    public IEnumerator<IConsumer> GetEnumerator() => _consumers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public ValueTask DisposeAsync() => this.DisposeAllAsync();
}
