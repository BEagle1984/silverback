// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;

namespace Silverback.Tests.Types;

public sealed class TestClient : IBrokerClient
{
    public string Name => "client1-name";

    public string DisplayName => "client1";

    public AsyncEvent<BrokerClient> Initializing { get; } = new();

    public AsyncEvent<BrokerClient> Initialized { get; } = new();

    public AsyncEvent<BrokerClient> Disconnecting { get; } = new();

    public AsyncEvent<BrokerClient> Disconnected { get; } = new();

    public ClientStatus Status => ClientStatus.Initializing;

    public void Dispose() => throw new NotSupportedException();

    public ValueTask DisposeAsync() => throw new NotSupportedException();

    public ValueTask ConnectAsync() => throw new NotSupportedException();

    public ValueTask DisconnectAsync() => throw new NotSupportedException();

    public ValueTask ReconnectAsync() => throw new NotSupportedException();
}
