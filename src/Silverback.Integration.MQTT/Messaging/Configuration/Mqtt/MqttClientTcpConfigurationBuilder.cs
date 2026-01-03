// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Net;
using System.Net.Sockets;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     Builds the <see cref="MqttClientTcpConfiguration" />.
/// </summary>
public class MqttClientTcpConfigurationBuilder
{
    private MqttClientTcpConfiguration _tcpConfiguration = new();

    /// <summary>
    ///     Sets the address family of the underlying socket.
    /// </summary>
    /// <param name="addressFamily">
    ///     The address family.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTcpConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTcpConfigurationBuilder WithAddressFamily(AddressFamily addressFamily)
    {
        _tcpConfiguration = _tcpConfiguration with { AddressFamily = addressFamily };
        return this;
    }

    /// <summary>
    ///     Sets the buffer size of the underlying socket.
    /// </summary>
    /// <param name="bufferSize">
    ///     The buffer size in bytes.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTcpConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTcpConfigurationBuilder WithBufferSize(int bufferSize)
    {
        _tcpConfiguration = _tcpConfiguration with { BufferSize = bufferSize };
        return this;
    }

    /// <summary>
    ///     Enables dual-mode on the underlying socket (IPv4 &amp; IPv6).
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTcpConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTcpConfigurationBuilder EnableDualMode()
    {
        _tcpConfiguration = _tcpConfiguration with { DualMode = true };
        return this;
    }

    /// <summary>
    ///     Disables dual-mode on the underlying socket.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTcpConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTcpConfigurationBuilder DisableDualMode()
    {
        _tcpConfiguration = _tcpConfiguration with { DualMode = false };
        return this;
    }

    /// <summary>
    ///     Enables the linger option on the underlying socket and sets the linger time in seconds.
    /// </summary>
    /// <param name="seconds">
    ///     The linger time in seconds.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTcpConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTcpConfigurationBuilder EnableLinger(int seconds)
    {
        _tcpConfiguration = _tcpConfiguration with { LingerState = new LingerOption(true, seconds) };
        return this;
    }

    /// <summary>
    ///     Disables the linger option on the underlying socket.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTcpConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTcpConfigurationBuilder DisableLinger()
    {
        _tcpConfiguration = _tcpConfiguration with { LingerState = new LingerOption(false, 0) };
        return this;
    }

    /// <summary>
    ///     Sets the local endpoint (network interface) to be used by the client.
    /// </summary>
    /// <param name="localEndpoint">
    ///     The local <see cref="EndPoint" /> or <c>null</c> to let the OS pick.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTcpConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTcpConfigurationBuilder WithLocalEndpoint(EndPoint? localEndpoint)
    {
        _tcpConfiguration = _tcpConfiguration with { LocalEndpoint = localEndpoint };
        return this;
    }

    /// <summary>
    ///     Enables the TCP no-delay option (disable Nagle's algorithm).
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTcpConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTcpConfigurationBuilder EnableNoDelay()
    {
        _tcpConfiguration = _tcpConfiguration with { NoDelay = true };
        return this;
    }

    /// <summary>
    ///     Disables the TCP no-delay option (enable Nagle's algorithm).
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTcpConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTcpConfigurationBuilder DisableNoDelay()
    {
        _tcpConfiguration = _tcpConfiguration with { NoDelay = false };
        return this;
    }

    /// <summary>
    ///     Sets the protocol type used by the underlying socket (usually TCP).
    /// </summary>
    /// <param name="protocolType">
    ///     The <see cref="ProtocolType" /> to use.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTcpConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTcpConfigurationBuilder WithProtocolType(ProtocolType protocolType)
    {
        _tcpConfiguration = _tcpConfiguration with { ProtocolType = protocolType };
        return this;
    }

    /// <summary>
    ///     Sets the remote endpoint (server) to connect to.
    /// </summary>
    /// <param name="remoteEndpoint">
    ///     The remote <see cref="EndPoint" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTcpConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTcpConfigurationBuilder WithRemoteEndpoint(EndPoint? remoteEndpoint)
    {
        _tcpConfiguration = _tcpConfiguration with { RemoteEndpoint = remoteEndpoint };
        return this;
    }

    /// <summary>
    ///     Sets the remote endpoint (server) to connect to by creating a <see cref="DnsEndPoint" /> from the provided host and port.
    /// </summary>
    /// <param name="host">
    ///     The DNS host name or IP address as string.
    /// </param>
    /// <param name="port">
    ///     The port number.
    /// </param>
    /// <param name="addressFamily">
    ///     Optional address family; if <c>null</c> <see cref="AddressFamily.Unspecified" /> will be used.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTcpConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTcpConfigurationBuilder WithRemoteEndpoint(string host, int port, AddressFamily? addressFamily = null)
    {
        DnsEndPoint dnsEndPoint = new(host, port, addressFamily ?? AddressFamily.Unspecified);
        return WithRemoteEndpoint(dnsEndPoint);
    }

    /// <summary>
    ///     Builds the <see cref="MqttClientTcpConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTcpConfiguration" />.
    /// </returns>
    public MqttClientTcpConfiguration Build() => _tcpConfiguration;
}
