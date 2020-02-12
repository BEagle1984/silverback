// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    // TODO: Cleanup + test serialization + test equality
    public sealed class RabbitConnectionConfig : IEquatable<RabbitConnectionConfig>
    {
        private static readonly ConfigurationDictionaryComparer<string, object> ClientPropertiesComparer =
            new ConfigurationDictionaryComparer<string, object>();

        /// <summary>The AMQP URI SSL protocols.</summary>
        public SslProtocols? AmqpUriSslProtocols { get; set; }

        // /// <summary>SASL auth mechanisms to use.</summary>
        //public IList<AuthMechanismFactory> AuthMechanisms { get; set; } = ConnectionFactory.DefaultAuthMechanisms;

        /// <summary>
        ///     Set to false to disable automatic connection recovery.
        ///     Defaults to true.
        /// </summary>
        public bool? AutomaticRecoveryEnabled { get; set; }

        /// <summary>The host to connect to.</summary>
        public string HostName { get; set; }

        /// <summary>
        ///     Amount of time client will wait for before re-trying  to recover connection.
        /// </summary>
        public TimeSpan? NetworkRecoveryInterval { get; set; }

        /// <summary>
        ///     Amount of time protocol handshake operations are allowed to take before
        ///     timing out.
        /// </summary>
        public TimeSpan? HandshakeContinuationTimeout { get; set; }

        /// <summary>
        ///     Amount of time protocol  operations (e.g. <code>queue.declare</code>) are allowed to take before
        ///     timing out.
        /// </summary>
        public TimeSpan? ContinuationTimeout { get; set; }

        // /// <summary>
        // /// Factory function for creating the <see cref="T:RabbitMQ.Client.IEndpointResolver" />
        // /// used to generate a list of endpoints for the ConnectionFactory
        // /// to try in order.
        // /// The default value creates an instance of the <see cref="T:RabbitMQ.Client.DefaultEndpointResolver" />
        // /// using the list of endpoints passed in. The DefaultEndpointResolver shuffles the
        // /// provided list each time it is requested.
        // /// </summary>
        // public Func<IEnumerable<AmqpTcpEndpoint>, IEndpointResolver> EndpointResolverFactory { get; set; } =
        //     (Func<IEnumerable<AmqpTcpEndpoint>, IEndpointResolver>) (endpoints =>
        //         (IEndpointResolver) new DefaultEndpointResolver(endpoints));

        /// <summary>
        ///     The port to connect on. <see cref="F:RabbitMQ.Client.AmqpTcpEndpoint.UseDefaultPort" />
        ///     indicates the default for the protocol should be used.
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        ///     Timeout setting for connection attempts (in milliseconds).
        /// </summary>
        public int? RequestedConnectionTimeout { get; set; }

        /// <summary>
        ///     Timeout setting for socket read operations (in milliseconds).
        /// </summary>
        public int? SocketReadTimeout { get; set; }

        /// <summary>
        ///     Timeout setting for socket write operations (in milliseconds).
        /// </summary>
        public int? SocketWriteTimeout { get; set; }

        /// <summary>Ssl options setting.</summary>
        public RabbitSslOption Ssl { get; set; } = new RabbitSslOption();

        /// <summary>
        ///     Set to false to make automatic connection recovery not recover topology (exchanges, queues, bindings, etc).
        ///     Defaults to true.
        /// </summary>
        public bool? TopologyRecoveryEnabled { get; set; }

        /// <summary>
        ///     Dictionary of client properties to be sent to the server.
        /// </summary>
        public IDictionary<string, object> ClientProperties { get; set; } = new Dictionary<string, object>();

        /// <summary>Password to use when authenticating to the server.</summary>
        public string Password { get; set; }

        /// <summary>Maximum channel number to ask for.</summary>
        public ushort? RequestedChannelMax { get; set; }

        /// <summary>Frame-max parameter to ask for (in bytes).</summary>
        public uint? RequestedFrameMax { get; set; }

        /// <summary>
        ///     Heartbeat timeout to use when negotiating with the server (in seconds).
        /// </summary>
        public ushort? RequestedHeartbeat { get; set; }

        /// <summary>
        ///     When set to true, background thread will be used for the I/O loop.
        /// </summary>
        public bool? UseBackgroundThreadsForIO { get; set; }

        /// <summary>Username to use when authenticating to the server.</summary>
        public string UserName { get; set; }

        /// <summary>Virtual host to access during this connection.</summary>
        public string VirtualHost { get; set; }

        /// <summary>
        ///     Default client provided name to be used for connections.
        /// </summary>
        public string ClientProvidedName { get; set; }

        public void Validate()
        {
            // Nothing to validate?
        }

        #region Equality

        public bool Equals(RabbitConnectionConfig other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(AmqpUriSslProtocols, other.AmqpUriSslProtocols) &&
                   Equals(AutomaticRecoveryEnabled, other.AutomaticRecoveryEnabled) &&
                   string.Equals(HostName, other.HostName, StringComparison.InvariantCulture) &&
                   Nullable.Equals(NetworkRecoveryInterval, other.NetworkRecoveryInterval) &&
                   Nullable.Equals(HandshakeContinuationTimeout, other.HandshakeContinuationTimeout) &&
                   Nullable.Equals(ContinuationTimeout, other.ContinuationTimeout) &&
                   Equals(Port, other.Port) &&
                   Equals(RequestedConnectionTimeout, other.RequestedConnectionTimeout) &&
                   Equals(SocketReadTimeout, other.SocketReadTimeout) &&
                   Equals(SocketWriteTimeout, other.SocketWriteTimeout) &&
                   Equals(Ssl, other.Ssl) &&
                   Equals(TopologyRecoveryEnabled, other.TopologyRecoveryEnabled) &&
                   ClientPropertiesComparer.Equals(ClientProperties, other.ClientProperties) &&
                   string.Equals(Password, other.Password, StringComparison.InvariantCulture) &&
                   Equals(RequestedChannelMax, other.RequestedChannelMax) &&
                   Equals(RequestedFrameMax, other.RequestedFrameMax) &&
                   Equals(RequestedHeartbeat, other.RequestedHeartbeat) &&
                   Equals(UseBackgroundThreadsForIO, other.UseBackgroundThreadsForIO) &&
                   string.Equals(UserName, other.UserName, StringComparison.InvariantCulture) &&
                   string.Equals(VirtualHost, other.VirtualHost, StringComparison.InvariantCulture) &&
                   string.Equals(ClientProvidedName, other.ClientProvidedName, StringComparison.InvariantCulture);
        }

        // Workaround for the (ushort?)null==null returning false 
        // private bool Equals(ushort? x, ushort? y)
        // {
        //     if (!x.HasValue && !y.HasValue) return true;
        //     if (!x.HasValue || !y.HasValue) return false;
        //     return x.Value == y.Value;
        // }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RabbitConnectionConfig) obj);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = AmqpUriSslProtocols.GetHashCode();
                hashCode = (hashCode * 397) ^ AutomaticRecoveryEnabled.GetHashCode();
                hashCode = (hashCode * 397) ^
                           (HostName != null ? StringComparer.InvariantCulture.GetHashCode(HostName) : 0);
                hashCode = (hashCode * 397) ^ NetworkRecoveryInterval.GetHashCode();
                hashCode = (hashCode * 397) ^ HandshakeContinuationTimeout.GetHashCode();
                hashCode = (hashCode * 397) ^ ContinuationTimeout.GetHashCode();
                hashCode = (hashCode * 397) ^ Port.GetHashCode();
                hashCode = (hashCode * 397) ^ RequestedConnectionTimeout.GetHashCode();
                hashCode = (hashCode * 397) ^ SocketReadTimeout.GetHashCode();
                hashCode = (hashCode * 397) ^ SocketWriteTimeout.GetHashCode();
                hashCode = (hashCode * 397) ^ (Ssl != null ? Ssl.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ TopologyRecoveryEnabled.GetHashCode();
                hashCode = (hashCode * 397) ^ (ClientProperties != null ? ClientProperties.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^
                           (Password != null ? StringComparer.InvariantCulture.GetHashCode(Password) : 0);
                hashCode = (hashCode * 397) ^ RequestedChannelMax.GetHashCode();
                hashCode = (hashCode * 397) ^ RequestedFrameMax.GetHashCode();
                hashCode = (hashCode * 397) ^ RequestedHeartbeat.GetHashCode();
                hashCode = (hashCode * 397) ^ UseBackgroundThreadsForIO.GetHashCode();
                hashCode = (hashCode * 397) ^
                           (UserName != null ? StringComparer.InvariantCulture.GetHashCode(UserName) : 0);
                hashCode = (hashCode * 397) ^
                           (VirtualHost != null ? StringComparer.InvariantCulture.GetHashCode(VirtualHost) : 0);
                hashCode = (hashCode * 397) ^ (ClientProvidedName != null
                    ? StringComparer.InvariantCulture.GetHashCode(ClientProvidedName)
                    : 0);
                return hashCode;
            }
        }

        #endregion
    }
}