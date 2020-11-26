// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     The configuration used to connect with the RabbitMQ broker.
    /// </summary>
    public sealed class RabbitConnectionConfig : IEquatable<RabbitConnectionConfig>, IValidatableEndpointSettings
    {
        private static readonly ConfigurationDictionaryEqualityComparer<string, object>
            ClientPropertiesEqualityComparer = new();

        /// <summary>
        ///     Gets or sets the AMQP URI SSL protocols.
        /// </summary>
        public SslProtocols? AmqpUriSslProtocols { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the automatic connection recovery is enabled. The default is
        ///     <c>true</c>.
        /// </summary>
        public bool? AutomaticRecoveryEnabled { get; set; }

        /// <summary>
        ///     Gets or sets the name of th e host to connect to.
        /// </summary>
        public string? HostName { get; set; }

        /// <summary>
        ///     Gets or sets the amount of time the client will wait for before re-trying to recover the connection.
        /// </summary>
        public TimeSpan? NetworkRecoveryInterval { get; set; }

        /// <summary>
        ///     Gets or sets the amount of time protocol handshake operations are allowed to take before timing out.
        /// </summary>
        public TimeSpan? HandshakeContinuationTimeout { get; set; }

        /// <summary>
        ///     Gets or sets the amount of time the protocol operations (e.g. <code>queue.declare</code>) are
        ///     allowed to take before timing out.
        /// </summary>
        public TimeSpan? ContinuationTimeout { get; set; }

        /// <summary>
        ///     Gets or sets the port to connect on.
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        ///     Gets or sets the timeout setting for the connection attempts.
        /// </summary>
        public TimeSpan? RequestedConnectionTimeout { get; set; }

        /// <summary>
        ///     Gets or sets the timeout setting for the socket read operations.
        /// </summary>
        public TimeSpan? SocketReadTimeout { get; set; }

        /// <summary>
        ///     Gets or sets the timeout setting for the socket write operations.
        /// </summary>
        public TimeSpan? SocketWriteTimeout { get; set; }

        /// <summary>
        ///     Gets or sets the SSL options setting.
        /// </summary>
        public RabbitSslOption Ssl { get; set; } = new();

        /// <summary>
        ///     Gets or sets a value indicating whether the automatic connection recovery must recover recover also
        ///     topology (exchanges, queues, bindings, etc). Defaults to true.
        /// </summary>
        public bool? TopologyRecoveryEnabled { get; set; }

        /// <summary>
        ///     Gets or sets the dictionary of client properties to be sent to the server.
        /// </summary>
        [SuppressMessage("ReSharper", "CA2227", Justification = "DTO")]
        public IDictionary<string, object> ClientProperties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        ///     Gets or sets the password to use when authenticating to the server.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        ///     Gets or sets the maximum channel number to ask for.
        /// </summary>
        public ushort? RequestedChannelMax { get; set; }

        /// <summary>
        ///     Gets or sets the frame-max parameter to ask for (in bytes).
        /// </summary>
        public uint? RequestedFrameMax { get; set; }

        /// <summary>
        ///     Gets or sets the heartbeat timeout to use when negotiating with the server.
        /// </summary>
        public TimeSpan? RequestedHeartbeat { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether a background thread will be used for the I/O loop.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "IO is correct")]
        public bool? UseBackgroundThreadsForIO { get; set; }

        /// <summary>
        ///     Gets or sets the username to use when authenticating to the server.
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        ///     Gets or sets the virtual host to access during this connection.
        /// </summary>
        public string? VirtualHost { get; set; }

        /// <summary>
        ///     Gets or sets the default client provided name to be used for connections.
        /// </summary>
        public string? ClientProvidedName { get; set; }

        /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
        public void Validate()
        {
            // Nothing to validate?
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(RabbitConnectionConfig? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Equals(AmqpUriSslProtocols, other.AmqpUriSslProtocols) &&
                   Equals(AutomaticRecoveryEnabled, other.AutomaticRecoveryEnabled) &&
                   string.Equals(HostName, other.HostName, StringComparison.Ordinal) &&
                   Nullable.Equals(NetworkRecoveryInterval, other.NetworkRecoveryInterval) &&
                   Nullable.Equals(HandshakeContinuationTimeout, other.HandshakeContinuationTimeout) &&
                   Nullable.Equals(ContinuationTimeout, other.ContinuationTimeout) &&
                   Equals(Port, other.Port) &&
                   Equals(RequestedConnectionTimeout, other.RequestedConnectionTimeout) &&
                   Equals(SocketReadTimeout, other.SocketReadTimeout) &&
                   Equals(SocketWriteTimeout, other.SocketWriteTimeout) &&
                   Equals(Ssl, other.Ssl) &&
                   Equals(TopologyRecoveryEnabled, other.TopologyRecoveryEnabled) &&
                   ClientPropertiesEqualityComparer.Equals(ClientProperties, other.ClientProperties) &&
                   string.Equals(Password, other.Password, StringComparison.Ordinal) &&
                   Equals(RequestedChannelMax, other.RequestedChannelMax) &&
                   Equals(RequestedFrameMax, other.RequestedFrameMax) &&
                   Equals(RequestedHeartbeat, other.RequestedHeartbeat) &&
                   Equals(UseBackgroundThreadsForIO, other.UseBackgroundThreadsForIO) &&
                   string.Equals(UserName, other.UserName, StringComparison.Ordinal) &&
                   string.Equals(VirtualHost, other.VirtualHost, StringComparison.Ordinal) &&
                   string.Equals(ClientProvidedName, other.ClientProvidedName, StringComparison.Ordinal);
        }

        /// <inheritdoc cref="object.Equals(object)" />
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((RabbitConnectionConfig)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = Justifications.Settings)]
        public override int GetHashCode()
        {
            var hashCode = default(HashCode);

            hashCode.Add(AmqpUriSslProtocols);
            hashCode.Add(AutomaticRecoveryEnabled);
            hashCode.Add(HostName);
            hashCode.Add(NetworkRecoveryInterval);
            hashCode.Add(HandshakeContinuationTimeout);
            hashCode.Add(ContinuationTimeout);
            hashCode.Add(Port);
            hashCode.Add(RequestedConnectionTimeout);
            hashCode.Add(SocketReadTimeout);
            hashCode.Add(SocketWriteTimeout);
            hashCode.Add(Ssl);
            hashCode.Add(TopologyRecoveryEnabled);
            hashCode.Add(ClientProperties);
            hashCode.Add(Password);
            hashCode.Add(RequestedChannelMax);
            hashCode.Add(RequestedFrameMax);
            hashCode.Add(RequestedHeartbeat);
            hashCode.Add(UseBackgroundThreadsForIO);
            hashCode.Add(UserName);
            hashCode.Add(VirtualHost);
            hashCode.Add(ClientProvidedName);

            return hashCode.ToHashCode();
        }
    }
}
