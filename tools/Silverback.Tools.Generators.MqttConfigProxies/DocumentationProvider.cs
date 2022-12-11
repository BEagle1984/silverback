// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using MQTTnet.Client.Options;

namespace Silverback.Tools.Generators.MqttConfigProxies;

[SuppressMessage("ReSharper", "CognitiveComplexity", Justification = "Don't care")]
internal static class DocumentationProvider
{
    public static string GetSummary(PropertyInfo property)
    {
        switch (property.DeclaringType!.Name)
        {
            case nameof(MqttClientOptions):
                return GetSummaryForMqttClientOptions(property);
            case nameof(MqttClientCredentials):
                return GetSummaryForMqttClientCredentials(property);
            case nameof(MqttClientTcpOptions):
                return GetSummaryForMqttClientTcpOptions(property);
            case nameof(MqttClientWebSocketOptions):
                return GetSummaryForMqttClientWebSocketOptions(property);
            case nameof(MqttClientTlsOptions):
                return GetSummaryForMqttClientTlsOptions(property);
            case nameof(MqttClientWebSocketProxyOptions):
                return GetSummaryForMqttClientWebSocketProxyOptions(property);
            default:
                return string.Empty;
        }
    }

    private static string GetSummaryForMqttClientOptions(PropertyInfo property)
    {
        StringBuilder builder = new();

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
            case nameof(MqttClientOptions.ExtendedAuthenticationExchangeHandler):
                builder.AppendLine("    ///     Gets the handler to be used to handle the custom authentication data exchange.");
                break;
            case nameof(MqttClientOptions.ProtocolVersion):
                builder.AppendLine("    ///     Gets the MQTT protocol version. The default is <see cref=\"MQTTnet.Formatter.MqttProtocolVersion.V500\" />.");
                break;
            case nameof(MqttClientOptions.ChannelOptions):
                builder.AppendLine("    ///     Gets the channel options (either <see cref=\"MqttClientTcpOptions\" /> or <see cref=\"MqttClientWebSocketOptions\" />.");
                break;
            case nameof(MqttClientOptions.CommunicationTimeout):
                builder.AppendLine("    ///     Gets the maximum period that can elapse without a packet being sent to the message broker.");
                builder.AppendLine("    ///     When this period is elapsed a ping packet will be sent to keep the connection alive. The default is 15 seconds.");
                break;
            case nameof(MqttClientOptions.KeepAlivePeriod):
                builder.AppendLine("    ///     Gets the communication timeout. The default is 10 seconds.");
                break;
            case nameof(MqttClientOptions.WillMessage):
                builder.AppendLine("    ///     Gets the last will message to be sent when the client disconnects ungracefully.");
                break;
            case nameof(MqttClientOptions.WillDelayInterval):
                builder.AppendLine("    ///     Gets the number of seconds to wait before sending the last will message. If the client reconnects between this interval the");
                builder.AppendLine("    ///     Gets the number of seconds to wait before sending the last will message. If the client message will not be sent.");
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
            case nameof(MqttClientOptions.PacketInspector):
                builder.AppendLine("    ///     Gets <see cref=\"IMqttPacketInspector\"/> that will be used to inspect packets before they are sent and after they are received.");
                break;
            default:
                return string.Empty;
        }

        return builder.ToString();
    }

    private static string GetSummaryForMqttClientCredentials(PropertyInfo property)
    {
        StringBuilder builder = new();

        switch (property.Name)
        {
            case nameof(MqttClientCredentials.Username):
                builder.AppendLine("    /// Gets the username.");
                break;
            case nameof(MqttClientCredentials.Password):
                builder.AppendLine("    /// Gets the password.");
                break;
            default:
                return string.Empty;
        }

        return builder.ToString();
    }

    private static string GetSummaryForMqttClientTcpOptions(PropertyInfo property)
    {
        StringBuilder builder = new();

        switch (property.Name)
        {
            case nameof(MqttClientTcpOptions.Server):
                builder.AppendLine("    /// Gets the server name or address.");
                break;
            case nameof(MqttClientTcpOptions.Port):
                builder.AppendLine("    /// Gets the server port.");
                break;
            case nameof(MqttClientTcpOptions.BufferSize):
                builder.AppendLine("    /// Gets the size of both the receive and send buffers of the underlying <see cref=\"Socket\" />.");
                break;
            case nameof(MqttClientTcpOptions.DualMode):
                builder.AppendLine("    /// Gets a value that specifies whether the underlying <see cref=\"Socket\" /> is a dual-mode socket used for both IPv4 and IPv6.");
                break;
            case nameof(MqttClientTcpOptions.NoDelay):
                builder.AppendLine("    /// Gets a value indicating whether the underlying <see cref=\"Socket\" /> is a dual-mode socket used for both IPv4 and IPv6.");
                break;
            case nameof(MqttClientTcpOptions.AddressFamily):
                builder.AppendLine("    /// Gets the address family of the underlying <see cref=\"Socket\" />.");
                break;
            default:
                return string.Empty;
        }

        return builder.ToString();
    }

    private static string GetSummaryForMqttClientWebSocketOptions(PropertyInfo property)
    {
        StringBuilder builder = new();

        switch (property.Name)
        {
            case nameof(MqttClientWebSocketOptions.Uri):
                builder.AppendLine("    /// Gets the server URI.");
                break;
            case nameof(MqttClientWebSocketOptions.RequestHeaders):
                builder.AppendLine("    /// Gets the HTTP request headers.");
                break;
            case nameof(MqttClientWebSocketOptions.SubProtocols):
                builder.AppendLine("    /// Gets the sub-protocols to be negotiated during the WebSocket connection handshake.");
                break;
            case nameof(MqttClientWebSocketOptions.CookieContainer):
                builder.AppendLine("    /// Gets the cookies associated with the request.");
                break;
            default:
                return string.Empty;
        }

        return builder.ToString();
    }

    private static string GetSummaryForMqttClientTlsOptions(PropertyInfo property)
    {
        StringBuilder builder = new();

        switch (property.Name)
        {
            case nameof(MqttClientTlsOptions.UseTls):
                builder.AppendLine("    /// Gets a value indicating whether the client should use TLS.");
                break;
            case nameof(MqttClientTlsOptions.IgnoreCertificateRevocationErrors):
                builder.AppendLine("    /// Gets a value indicating whether the client should ignore the certificate revocation errors.");
                break;
            case nameof(MqttClientTlsOptions.IgnoreCertificateChainErrors):
                builder.AppendLine("    /// Gets a value indicating whether the client should ignore the certificate chain errors.");
                break;
            case nameof(MqttClientTlsOptions.AllowUntrustedCertificates):
                builder.AppendLine("    /// Gets a value indicating whether the client should accept untrusted certificates.");
                break;
            case nameof(MqttClientTlsOptions.SslProtocol):
                builder.AppendLine("    /// Gets the protocol to be used. The default is TLS 1.3 ");
                builder.AppendLine("    /// (or TLS 1.2 for older .NET versions).");
                break;
            case nameof(MqttClientTlsOptions.CertificateValidationHandler):
                builder.AppendLine("    /// Gets the function to be used to validate the remote certificate.");
                break;
            default:
                return string.Empty;
        }

        return builder.ToString();
    }

    private static string GetSummaryForMqttClientWebSocketProxyOptions(PropertyInfo property)
    {
        StringBuilder builder = new();

        switch (property.Name)
        {
            case nameof(MqttClientWebSocketProxyOptions.Address):
                builder.AppendLine("    /// Gets the proxy address.");
                break;
            case nameof(MqttClientWebSocketProxyOptions.Username):
                builder.AppendLine("    /// Gets the username to be used to authenticate with the proxy server.");
                break;
            case nameof(MqttClientWebSocketProxyOptions.Password):
                builder.AppendLine("    /// Gets the password to be used to authenticate with the proxy server.");
                break;
            case nameof(MqttClientWebSocketProxyOptions.Domain):
                builder.AppendLine("    /// Gets proxy server domain.");
                break;
            case nameof(MqttClientWebSocketProxyOptions.BypassOnLocal):
                builder.AppendLine("    /// Gets a value indicating whether the proxy should be bypassed for local calls.");
                break;
            case nameof(MqttClientWebSocketProxyOptions.BypassList):
                builder.AppendLine("    /// Gets the bypass list.");
                break;
            default:
                return string.Empty;
        }

        return builder.ToString();
    }
}
