// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using MQTTnet;

namespace Silverback.Tools.Generators.MqttConfigProxies;

[SuppressMessage("ReSharper", "CognitiveComplexity", Justification = "Don't care")]
[SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity", Justification = "Don't care")]
public static class DocumentationProvider
{
    public static void AppendSummary(this StringBuilder builder, PropertyInfo property)
    {
        builder.AppendLine("    /// <summary>");

        switch (property.DeclaringType!.Name)
        {
            case nameof(MqttClientOptions):
                AppendSummaryForMqttClientOptions(builder, property);
                break;
            case nameof(MqttClientTcpOptions):
                AppendSummaryForMqttClientTcpOptions(builder, property);
                break;
            case nameof(MqttClientWebSocketOptions):
                AppendSummaryForMqttClientWebSocketOptions(builder, property);
                break;
            case nameof(MqttClientTlsOptions):
                AppendSummaryForMqttClientTlsOptions(builder, property);
                break;
            case nameof(MqttClientWebSocketProxyOptions):
                AppendSummaryForMqttClientWebSocketProxyOptions(builder, property);
                break;
        }

        builder.AppendLine("    /// </summary>");
    }

    private static void AppendSummaryForMqttClientOptions(StringBuilder builder, PropertyInfo property)
    {
        switch (property.Name)
        {
            case nameof(MqttClientOptions.ClientId):
                builder.AppendLine("    /// Gets the client identifier. The default is <c>Guid.NewGuid().ToString()</c>.");
                break;
            case nameof(MqttClientOptions.CleanSession):
                builder.AppendLine("    /// Gets a value indicating whether a clean non-persistent session has to be created for this client. The default is <c>true</c>.");
                break;
            case nameof(MqttClientOptions.Credentials):
                builder.AppendLine("    ///     Gets the credentials to be used to authenticate with the message broker.");
                break;
            case nameof(MqttClientOptions.ProtocolVersion):
                builder.AppendLine("    ///     Gets the MQTT protocol version. The default is <see cref=\"MQTTnet.Formatter.MqttProtocolVersion.V500\" />.");
                break;
            case nameof(MqttClientOptions.ChannelOptions):
                builder.AppendLine("    ///     Gets the channel options (either <see cref=\"MqttClientTcpOptions\" /> or <see cref=\"MqttClientWebSocketOptions\" />.");
                break;
            case nameof(MqttClientOptions.KeepAlivePeriod):
                builder.AppendLine("    ///     Gets the communication timeout. The default is 10 seconds.");
                break;
            case nameof(MqttClientOptions.AuthenticationMethod):
                builder.AppendLine("    ///     Gets the custom authentication method.");
                break;
            case nameof(MqttClientOptions.AuthenticationData):
                builder.AppendLine("    ///    Gets the authentication data to be used for the custom authentication.");
                break;
            case nameof(MqttClientOptions.MaximumPacketSize):
                builder.AppendLine("    ///     Gets the maximum packet size in byte the client will process. The default is no limit.");
                break;
            case nameof(MqttClientOptions.ReceiveMaximum):
                builder.AppendLine("    ///     Gets the maximum number of QoS 1 and QoS 2 publications that can be received and processed concurrently. The default value is");
                builder.AppendLine("    ///     <c>null</c>, which means <c>65'535</c>.");
                break;
            case nameof(MqttClientOptions.RequestProblemInformation):
                builder.AppendLine("    ///     Gets a value indicating whether the reason string or user properties can be sent with any packet. The default is usually");
                builder.AppendLine("    ///     <c>true</c>.");
                break;
            case nameof(MqttClientOptions.RequestResponseInformation):
                builder.AppendLine("    ///     Gets a value indicating whether the server should return the response information in the <i>CONNACK</i> packet. The default is");
                builder.AppendLine("    ///     usually <c>false</c>.");
                break;
            case nameof(MqttClientOptions.SessionExpiryInterval):
                builder.AppendLine("    ///     Gets the session expiry interval in seconds. When set to 0 the session will expire when the connection is closed, while");
                builder.AppendLine("    ///     <see cref=\"uint.MaxValue\" /> indicates that the session will never expire. The default is 0.");
                break;
            case nameof(MqttClientOptions.TopicAliasMaximum):
                builder.AppendLine("    ///     Gets the maximum number of topic aliases the server can send in the <i>PUBLISH</i> packet. The default is 0, meaning that no");
                builder.AppendLine("    ///     alias can be sent.");
                break;
            case nameof(MqttClientOptions.Timeout):
                builder.AppendLine("    ///     Gets the timeout which will be applied at socket level and internal operations.");
                builder.AppendLine("    ///     The default value is the same as for sockets in .NET in general.");
                break;
            case nameof(MqttClientOptions.TryPrivate):
                builder.AppendLine("    ///     Gets a value indicating whether the bridge must attempt to indicate to the remote broker that it is a bridge and not an ordinary");
                builder.AppendLine("    ///     client. If successful, this means that the loop detection will be more effective and that the retained messages will be propagated");
                builder.AppendLine("    ///     correctly. Not all brokers support this feature, so it may be necessary to set it to <c>false</c> if your bridge does not connect");
                builder.AppendLine("    ///     properly.");
                break;
            case nameof(MqttClientOptions.WriterBufferSize):
                builder.AppendLine("    ///     Gets the default and initial size of the packet write buffer. It is recommended to set this to a value close to the usual expected");
                builder.AppendLine("    ///     packet size * 1.5. Do not change this value when no memory issues are experienced.");
                break;
            case nameof(MqttClientOptions.WriterBufferSizeMax):
                builder.AppendLine("    ///     Gets the maximum size of the buffer writer. The writer will reduce its internal buffer to this value after serializing a");
                builder.AppendLine("    ///     packet. Do not change this value when no memory issues are experienced.");
                break;
            case nameof(MqttClientOptions.AllowPacketFragmentation):
                builder.AppendLine("    ///     Gets a value indicating whether the broker allows packet fragmentation.");
                builder.AppendLine("    ///     Unfortunately not all brokers (like AWS) do support fragmentation and will close the connection when receiving such packets.");
                builder.AppendLine("    ///     If such a service is used this flag must be set to <c>false</c>.");
                builder.AppendLine("    ///     The default is <c>true</c>.");
                break;
            case nameof(MqttClientOptions.ValidateFeatures):
                builder.AppendLine("    ///     Gets a value indicating whether the client should check if the configuration is valid for the selected protocol version.");
                builder.AppendLine("    ///     The default is <c>true</c>.");
                break;
            case nameof(MqttClientOptions.EnhancedAuthenticationHandler):
                builder.AppendLine("    ///     Gets the handler for AUTH packets. This can happen when connecting or at any time while being already connected.");
                break;
        }
    }

    private static void AppendSummaryForMqttClientTcpOptions(StringBuilder builder, PropertyInfo property)
    {
        switch (property.Name)
        {
            case nameof(MqttClientTcpOptions.RemoteEndpoint):
                builder.AppendLine("    ///     Gets the remote endpoint (server).");
                break;
            case nameof(MqttClientTcpOptions.BufferSize):
                builder.AppendLine("    ///     Gets the size of both the receive and send buffers of the underlying <see cref=\"Socket\" />.");
                break;
            case nameof(MqttClientTcpOptions.DualMode):
                builder.AppendLine("    ///     Gets a value that specifies whether the underlying <see cref=\"Socket\" /> is a dual-mode socket used for both IPv4 and IPv6.");
                break;
            case nameof(MqttClientTcpOptions.NoDelay):
                builder.AppendLine("    ///     Gets a value indicating whether the underlying <see cref=\"Socket\" /> is a dual-mode socket used for both IPv4 and IPv6.");
                break;
            case nameof(MqttClientTcpOptions.AddressFamily):
                builder.AppendLine("    ///     Gets the address family of the underlying <see cref=\"Socket\" />.");
                break;
            case nameof(MqttClientTcpOptions.LocalEndpoint):
                builder.AppendLine("    ///     Gets the local endpoint (network card) which is used by the client. If <c>null</c> the OS will select the network card.");
                break;
            case nameof(MqttClientTcpOptions.LingerState):
                builder.AppendLine("    ///     Gets the <see cref=\"System.Net.Sockets.LingerOption\" />.");
                break;
            case nameof(MqttClientTcpOptions.ProtocolType):
                builder.AppendLine("    ///     Gets the protocol type, usually TCP but when using other endpoint types like unix sockets it must be changed (IP for unix sockets).");
                builder.AppendLine("    ///     The default is <see cref=\"ProtocolType.Tcp\" />.");
                break;
        }
    }

    private static void AppendSummaryForMqttClientWebSocketOptions(StringBuilder builder, PropertyInfo property)
    {
        switch (property.Name)
        {
            case nameof(MqttClientWebSocketOptions.Uri):
                builder.AppendLine("    ///     Gets the server URI.");
                break;
            case nameof(MqttClientWebSocketOptions.RequestHeaders):
                builder.AppendLine("    ///     Gets the request headers.");
                break;
            case nameof(MqttClientWebSocketOptions.SubProtocols):
                builder.AppendLine("    ///     Gets the sub-protocols to be negotiated during the WebSocket connection handshake.");
                break;
            case nameof(MqttClientWebSocketOptions.CookieContainer):
                builder.AppendLine("    ///     Gets the cookies associated with the request.");
                break;
            case nameof(MqttClientWebSocketOptions.Credentials):
                builder.AppendLine("    ///     Gets the credentials to be used.");
                break;
            case nameof(MqttClientWebSocketOptions.UseDefaultCredentials):
                builder.AppendLine("    ///     Gets a value indicating whether the default (system) credentials should be used. The default is <c>false</c>.");
                break;
            case nameof(MqttClientWebSocketOptions.KeepAliveInterval):
                builder.AppendLine("    ///     Gets the keep alive interval for the web socket connection. This is not related to the keep alive interval for the MQTT protocol.");
                builder.AppendLine("    ///     The default is <see cref=\"WebSocket.DefaultKeepAliveInterval\" />.");
                break;
            case nameof(MqttClientWebSocketOptions.DangerousDeflateOptions):
                builder.AppendLine("    ///     Gets the <see cref=\"WebSocketDeflateOptions\" />.");
                break;
        }
    }

    private static void AppendSummaryForMqttClientTlsOptions(StringBuilder builder, PropertyInfo property)
    {
        switch (property.Name)
        {
            case nameof(MqttClientTlsOptions.UseTls):
                builder.AppendLine("    ///     Gets a value indicating whether the client should use TLS.");
                break;
            case nameof(MqttClientTlsOptions.IgnoreCertificateRevocationErrors):
                builder.AppendLine("    ///     Gets a value indicating whether the client should ignore the certificate revocation errors.");
                break;
            case nameof(MqttClientTlsOptions.IgnoreCertificateChainErrors):
                builder.AppendLine("    ///     Gets a value indicating whether the client should ignore the certificate chain errors.");
                break;
            case nameof(MqttClientTlsOptions.AllowUntrustedCertificates):
                builder.AppendLine("    ///     Gets a value indicating whether the client should accept untrusted certificates.");
                break;
            case nameof(MqttClientTlsOptions.SslProtocol):
                builder.AppendLine("    ///     Gets the protocol to be used. The default is TLS 1.3 or TLS 1.2.");
                break;
            case nameof(MqttClientTlsOptions.CertificateValidationHandler):
                builder.AppendLine("    ///     Gets the function to be used to validate the remote certificate.");
                break;
            case nameof(MqttClientTlsOptions.RevocationMode):
                builder.AppendLine("    ///     Gets the <see cref=\"System.Security.Cryptography.X509Certificates.X509RevocationMode\" />.");
                break;
            case nameof(MqttClientTlsOptions.CertificateSelectionHandler):
                builder.AppendLine("    ///     Gets the function to be used to select the client certificate.");
                break;
            case nameof(MqttClientTlsOptions.ClientCertificatesProvider):
                builder.AppendLine("    ///     Gets the provider to be used to get the client certificates.");
                break;
            case nameof(MqttClientTlsOptions.EncryptionPolicy):
                builder.AppendLine("    ///     Gets the <see cref=\"System.Net.Security.EncryptionPolicy\" />.");
                break;
            case nameof(MqttClientTlsOptions.TargetHost):
                builder.AppendLine("    ///     Gets the target host. If the value is <c>null</c> or empty the same host as the TCP socket host will be used.");
                break;
        }
    }

    private static void AppendSummaryForMqttClientWebSocketProxyOptions(StringBuilder builder, PropertyInfo property)
    {
        switch (property.Name)
        {
            case nameof(MqttClientWebSocketProxyOptions.Address):
                builder.AppendLine("    ///     Gets the proxy address.");
                break;
            case nameof(MqttClientWebSocketProxyOptions.Username):
                builder.AppendLine("    ///     Gets the username to be used to authenticate with the proxy server.");
                break;
            case nameof(MqttClientWebSocketProxyOptions.Password):
                builder.AppendLine("    ///     Gets the password to be used to authenticate with the proxy server.");
                break;
            case nameof(MqttClientWebSocketProxyOptions.Domain):
                builder.AppendLine("    ///     Gets proxy server domain.");
                break;
            case nameof(MqttClientWebSocketProxyOptions.BypassOnLocal):
                builder.AppendLine("    ///     Gets a value indicating whether the proxy should be bypassed for local calls.");
                break;
            case nameof(MqttClientWebSocketProxyOptions.BypassList):
                builder.AppendLine("    ///     Gets the bypass list.");
                break;
            case nameof(MqttClientWebSocketProxyOptions.UseDefaultCredentials):
                builder.AppendLine("    ///     Gets a value indicating whether the default (system) credentials should be used.");
                break;
        }
    }
}
