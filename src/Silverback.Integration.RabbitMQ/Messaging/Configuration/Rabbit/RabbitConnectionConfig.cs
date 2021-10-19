// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Rabbit
{
    /// <summary>
    ///     The configuration used to connect with the RabbitMQ broker.
    /// </summary>
    public sealed class RabbitConnectionConfig : IEquatable<RabbitConnectionConfig>, IValidatableEndpointSettings
    {
        private static readonly ConfigurationDictionaryEqualityComparer<string, object>
            ClientPropertiesEqualityComparer = new();

        /// <summary>
        ///     Gets the AMQP URI SSL protocols.
        /// </summary>
        public SslProtocols? AmqpUriSslProtocols { get; init; }

        /// <summary>
        ///     Gets a value indicating whether the automatic connection recovery is enabled. The default is
        ///     <c>true</c>.
        /// </summary>
        public bool? AutomaticRecoveryEnabled { get; init; }

        /// <summary>
        ///     Gets the name of th e host to connect to.
        /// </summary>
        public string? HostName { get; init; }

        /// <summary>
        ///     Gets the amount of time the client will wait for before re-trying to recover the connection.
        /// </summary>
        public TimeSpan? NetworkRecoveryInterval { get; init; }

        /// <summary>
        ///     Gets the amount of time protocol handshake operations are allowed to take before timing out.
        /// </summary>
        public TimeSpan? HandshakeContinuationTimeout { get; init; }

        /// <summary>
        ///     Gets the amount of time the protocol operations (e.g. <code>queue.declare</code>) are
        ///     allowed to take before timing out.
        /// </summary>
        public TimeSpan? ContinuationTimeout { get; init; }

        /// <summary>
        ///     Gets the port to connect on.
        /// </summary>
        public int? Port { get; init; }

        /// <summary>
        ///     Gets the timeout setting for the connection attempts.
        /// </summary>
        public TimeSpan? RequestedConnectionTimeout { get; init; }

        /// <summary>
        ///     Gets the timeout setting for the socket read operations.
        /// </summary>
        public TimeSpan? SocketReadTimeout { get; init; }

        /// <summary>
        ///     Gets the timeout setting for the socket write operations.
        /// </summary>
        public TimeSpan? SocketWriteTimeout { get; init; }

        /// <summary>
        ///     Gets the SSL options setting.
        /// </summary>
        public RabbitSslOption Ssl { get; init; } = new();

        /// <summary>
        ///     Gets a value indicating whether the automatic connection recovery must recover recover also
        ///     topology (exchanges, queues, bindings, etc). Defaults to true.
        /// </summary>
        public bool? TopologyRecoveryEnabled { get; init; }

        /// <summary>
        ///     Gets the dictionary of client properties to be sent to the server.
        /// </summary>
        public IReadOnlyDictionary<string, object> ClientProperties { get; init; } = new Dictionary<string, object>();

        /// <summary>
        ///     Gets the password to use when authenticating to the server.
        /// </summary>
        public string? Password { get; init; }

        /// <summary>
        ///     Gets the maximum channel number to ask for.
        /// </summary>
        public ushort? RequestedChannelMax { get; init; }

        /// <summary>
        ///     Gets the frame-max parameter to ask for (in bytes).
        /// </summary>
        public uint? RequestedFrameMax { get; init; }

        /// <summary>
        ///     Gets the heartbeat timeout to use when negotiating with the server.
        /// </summary>
        public TimeSpan? RequestedHeartbeat { get; init; }

        /// <summary>
        ///     Gets a value indicating whether a background thread will be used for the I/O loop.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "IO is correct")]
        public bool? UseBackgroundThreadsForIO { get; init; }

        /// <summary>
        ///     Gets the username to use when authenticating to the server.
        /// </summary>
        public string? UserName { get; init; }

        /// <summary>
        ///     Gets the virtual host to access during this connection.
        /// </summary>
        public string? VirtualHost { get; init; }

        /// <summary>
        ///     Gets the default client provided name to be used for connections.
        /// </summary>
        public string? ClientProvidedName { get; init; }

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
        public override int GetHashCode() => HashCode.Combine(HostName);
    }
}
