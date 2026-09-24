// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Tests.Extended.Stress.TestHost.Mqtt;

internal sealed class DisconnectableTcpProxy : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);

    private readonly CancellationTokenSource _stopping = new();

    private readonly ConcurrentBag<TcpClient> _clients = [];

    private readonly List<Task> _forwarding = [];

    private readonly Task _accepting;

    public DisconnectableTcpProxy()
    {
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _accepting = AcceptConnectionsAsync();
    }

    public int Port { get; }

    public void DropConnections()
    {
        foreach (TcpClient client in _clients)
        {
            client.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stopping.CancelAsync();
        _listener.Stop();
        await _accepting;
        DropConnections();
        await Task.WhenAll(_forwarding);
        _stopping.Dispose();
    }

    private async Task AcceptConnectionsAsync()
    {
        try
        {
            while (!_stopping.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(_stopping.Token);
                _forwarding.Add(ForwardConnectionAsync(client));
            }
        }
        catch (OperationCanceledException) when (_stopping.IsCancellationRequested)
        {
            // Stopping the proxy cancels its accept loop
        }
    }

    private async Task ForwardConnectionAsync(TcpClient client)
    {
        using (client)
        using (TcpClient server = new())
        {
            _clients.Add(client);
            _clients.Add(server);

            try
            {
                await server.ConnectAsync(MqttFixture.BrokerHost, 1883, _stopping.Token);
                Task outbound = CopyAsync(client.GetStream(), server.GetStream());
                Task inbound = CopyAsync(server.GetStream(), client.GetStream());
                await Task.WhenAny(outbound, inbound);

                client.Dispose();
                server.Dispose();
                await Task.WhenAll(outbound, inbound);
            }
            catch (SocketException)
            {
                // A cut connection can fail while establishing its forwarding socket
            }
            catch (ObjectDisposedException)
            {
                // A test can cut the connection before forwarding starts
            }
            catch (OperationCanceledException) when (_stopping.IsCancellationRequested)
            {
                // Disposing the proxy cancels outstanding connection attempts
            }
        }
    }

    private async Task CopyAsync(NetworkStream source, NetworkStream destination)
    {
        try
        {
            await source.CopyToAsync(destination, _stopping.Token);
        }
        catch (IOException)
        {
            // Dropping either socket interrupts pending reads and writes
        }
        catch (ObjectDisposedException)
        {
            // The opposite forwarding direction can close the socket first
        }
        catch (OperationCanceledException) when (_stopping.IsCancellationRequested)
        {
            // Disposing the proxy cancels outstanding forwarding operations
        }
    }
}
