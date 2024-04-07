// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

internal sealed class BrokerClientCollection : IReadOnlyCollection<IBrokerClient>, IDisposable
{
    private readonly ConcurrentBag<IBrokerClient> _clients = [];

    public int Count => _clients.Count;

    public void Add(IBrokerClient client) => _clients.Add(client);

    public ValueTask ConnectAllAsync() => _clients.ParallelForEachAsync(client => client.ConnectAsync());

    public ValueTask DisconnectAllAsync() => _clients.ParallelForEachAsync(client => client.DisconnectAsync());

    public void Dispose() => _clients.ForEach(client => client.Dispose());

    public IEnumerator<IBrokerClient> GetEnumerator() => _clients.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
