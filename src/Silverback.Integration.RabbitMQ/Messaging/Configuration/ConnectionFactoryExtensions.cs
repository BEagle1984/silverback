// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using RabbitMQ.Client;

namespace Silverback.Messaging.Configuration
{
    internal static class ConnectionFactoryExtensions
    {
        public static void ApplyConfiguration(this ConnectionFactory factory, RabbitConnectionConfig config)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (config == null) throw new ArgumentNullException(nameof(config));

            factory.ApplyConfigIfNotNull(config, c => c.AmqpUriSslProtocols, (f, v) => f.AmqpUriSslProtocols = v);
            factory.ApplyConfigIfNotNull(config, c => c.AutomaticRecoveryEnabled,
                (f, v) => f.AutomaticRecoveryEnabled = v);
            factory.ApplyConfigIfNotNull(config, c => c.HostName, (f, v) => f.HostName = v);
            factory.ApplyConfigIfNotNull(config, c => c.NetworkRecoveryInterval,
                (f, v) => f.NetworkRecoveryInterval = v);
            factory.ApplyConfigIfNotNull(config, c => c.HandshakeContinuationTimeout,
                (f, v) => f.HandshakeContinuationTimeout = v);
            factory.ApplyConfigIfNotNull(config, c => c.ContinuationTimeout, (f, v) => f.ContinuationTimeout = v);
            factory.ApplyConfigIfNotNull(config, c => c.Port, (f, v) => f.Port = v);
            factory.ApplyConfigIfNotNull(config, c => c.RequestedConnectionTimeout,
                (f, v) => f.RequestedConnectionTimeout = v);
            factory.ApplyConfigIfNotNull(config, c => c.SocketReadTimeout, (f, v) => f.SocketReadTimeout = v);
            factory.ApplyConfigIfNotNull(config, c => c.SocketWriteTimeout, (f, v) => f.SocketWriteTimeout = v);
            factory.ApplyConfigIfNotNull(config, c => c.TopologyRecoveryEnabled,
                (f, v) => f.TopologyRecoveryEnabled = v);
            factory.ApplyConfigIfNotNull(config, c => c.ClientProperties, (f, v) => f.ClientProperties = v);
            factory.ApplyConfigIfNotNull(config, c => c.Password, (f, v) => f.Password = v);
            factory.ApplyConfigIfNotNull(config, c => c.RequestedChannelMax, (f, v) => f.RequestedChannelMax = v);
            factory.ApplyConfigIfNotNull(config, c => c.RequestedFrameMax, (f, v) => f.RequestedFrameMax = v);
            factory.ApplyConfigIfNotNull(config, c => c.RequestedHeartbeat, (f, v) => f.RequestedHeartbeat = v);
            factory.ApplyConfigIfNotNull(config, c => c.UseBackgroundThreadsForIO,
                (f, v) => f.UseBackgroundThreadsForIO = v);
            factory.ApplyConfigIfNotNull(config, c => c.UserName, (f, v) => f.UserName = v);
            factory.ApplyConfigIfNotNull(config, c => c.VirtualHost, (f, v) => f.VirtualHost = v);
            factory.ApplyConfigIfNotNull(config, c => c.ClientProvidedName, (f, v) => f.ClientProvidedName = v);

            factory.Ssl.ApplyConfiguration(config.Ssl);
        }

        private static void ApplyConfiguration(this SslOption destination, RabbitSslOption source)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (source == null) throw new ArgumentNullException(nameof(source));

            destination.ApplyConfigIfNotNull(source, c => c.AcceptablePolicyErrors,
                (f, v) => f.AcceptablePolicyErrors = v);
            destination.ApplyConfigIfNotNull(source, c => c.CertPassphrase, (f, v) => f.CertPassphrase = v);
            destination.ApplyConfigIfNotNull(source, c => c.CertPath, (f, v) => f.CertPath = v);
            destination.ApplyConfigIfNotNull(source, c => c.CheckCertificateRevocation,
                (f, v) => f.CheckCertificateRevocation = v);
            destination.ApplyConfigIfNotNull(source, c => c.Enabled, (f, v) => f.Enabled = v);
            destination.ApplyConfigIfNotNull(source, c => c.ServerName, (f, v) => f.ServerName = v);

            destination.ApplyConfigIfNotNull(source, c => c.Version, (f, v) => f.Version = v);
        }

        private static void ApplyConfigIfNotNull<TSource, TDestination, TValue>(
            this TDestination destination,
            TSource source,
            Func<TSource, TValue> sourceFunction,
            Action<TDestination, TValue> applyAction)
        {
            var value = sourceFunction(source);

            if (value != null)
                applyAction(destination, value);
        }

        private static void ApplyConfigIfNotNull<TSource, TDestination, TValue>(
            this TDestination destination,
            TSource source,
            Func<TSource, TValue?> sourceFunction,
            Action<TDestination, TValue> applyAction)
            where TValue : struct
        {
            var value = sourceFunction(source);

            if (value.HasValue)
                applyAction(destination, value.Value);
        }
    }
}