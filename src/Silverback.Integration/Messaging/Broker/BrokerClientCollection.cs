// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

internal sealed class BrokerClientCollection : IBrokerClientCollection, IDisposable
{
    private readonly ConcurrentDictionary<string, IBrokerClient> _clients = [];

    public int Count => _clients.Count;

    public IBrokerClient this[string name] => _clients[name];

    public void Add(IBrokerClient client)
    {
        if (!_clients.TryAdd(client.Name, client))
            throw new InvalidOperationException($"A client with name '{client.Name}' has already been added.");
    }

    public ValueTask ConnectAllAsync() => _clients.Values.ParallelForEachAsync(client => client.ConnectAsync());

    public ValueTask DisconnectAllAsync() => _clients.Values.ParallelForEachAsync(client => client.DisconnectAsync());

    public void Dispose() => _clients.Values.ForEach(client => client.Dispose());

    [MustDisposeResource]
    public IEnumerator<IBrokerClient> GetEnumerator() => _clients.Values.GetEnumerator();

    [MustDisposeResource]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
