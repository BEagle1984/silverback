// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Tests.Integration.Kafka.TestTypes;

// A broker that is always down: it listens on the loopback interface and closes each connection right away, so that the
// librdkafka clients fail (and report it) immediately, without waiting for a connection timeout
public sealed class DisconnectingLoopbackServer : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly Task _acceptTask;

    public DisconnectingLoopbackServer()
    {
        _listener.Start();
        BootstrapServers = $"127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}";
        _acceptTask = Task.Run(AcceptAndCloseConnectionsAsync);
    }

    public string BootstrapServers { get; }

    public async ValueTask DisposeAsync()
    {
        await _cancellationTokenSource.CancelAsync();
        await _acceptTask;

        _listener.Dispose();
        _cancellationTokenSource.Dispose();
    }

    private async Task AcceptAndCloseConnectionsAsync()
    {
        try
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                using TcpClient client = await _listener.AcceptTcpClientAsync(_cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Disposed
        }
    }
}
