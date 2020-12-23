// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using RabbitMQ.Client;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Rabbit
{
    internal static class RabbitConnectionFactoryExtensions
    {
        public static void ApplyConfiguration(this ConnectionFactory connectionFactory, RabbitConnectionConfig config)
        {
            Check.NotNull(connectionFactory, nameof(connectionFactory));
            Check.NotNull(config, nameof(config));

            connectionFactory
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.AmqpUriSslProtocols,
                    (factory, value) => factory.AmqpUriSslProtocols = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.AutomaticRecoveryEnabled,
                    (factory, value) => factory.AutomaticRecoveryEnabled = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.HostName,
                    (factory, value) => factory.HostName = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.NetworkRecoveryInterval,
                    (factory, value) => factory.NetworkRecoveryInterval = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.HandshakeContinuationTimeout,
                    (factory, value) => factory.HandshakeContinuationTimeout = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.ContinuationTimeout,
                    (factory, value) => factory.ContinuationTimeout = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.Port,
                    (factory, value) => factory.Port = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.RequestedConnectionTimeout,
                    (factory, value) => factory.RequestedConnectionTimeout = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.SocketReadTimeout,
                    (factory, value) => factory.SocketReadTimeout = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.SocketWriteTimeout,
                    (factory, value) => factory.SocketWriteTimeout = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.TopologyRecoveryEnabled,
                    (factory, value) => factory.TopologyRecoveryEnabled = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.ClientProperties,
                    (factory, value) => factory.ClientProperties = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.Password,
                    (factory, value) => factory.Password = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.RequestedChannelMax,
                    (factory, value) => factory.RequestedChannelMax = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.RequestedFrameMax,
                    (factory, value) => factory.RequestedFrameMax = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.RequestedHeartbeat,
                    (factory, value) => factory.RequestedHeartbeat = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.UseBackgroundThreadsForIO,
                    (factory, value) => factory.UseBackgroundThreadsForIO = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.UserName,
                    (factory, value) => factory.UserName = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.VirtualHost,
                    (factory, value) => factory.VirtualHost = value)
                .ApplyConfigIfNotNull(
                    config,
                    sourceConfig => sourceConfig.ClientProvidedName,
                    (factory, value) => factory.ClientProvidedName = value)
                .Ssl.ApplyConfiguration(config.Ssl);
        }

        private static void ApplyConfiguration(this SslOption destination, RabbitSslOption source)
        {
            Check.NotNull(destination, nameof(destination));
            Check.NotNull(source, nameof(source));

            destination
                .ApplyConfigIfNotNull(
                    source,
                    sourceConfig => sourceConfig.AcceptablePolicyErrors,
                    (factory, value) => factory.AcceptablePolicyErrors = value)
                .ApplyConfigIfNotNull(
                    source,
                    sourceConfig => sourceConfig.CertPassphrase,
                    (factory, value) => factory.CertPassphrase = value)
                .ApplyConfigIfNotNull(
                    source,
                    sourceConfig => sourceConfig.CertPath,
                    (factory, value) => factory.CertPath = value)
                .ApplyConfigIfNotNull(
                    source,
                    sourceConfig => sourceConfig.CheckCertificateRevocation,
                    (factory, value) => factory.CheckCertificateRevocation = value)
                .ApplyConfigIfNotNull(
                    source,
                    sourceConfig => sourceConfig.Enabled,
                    (factory, value) => factory.Enabled = value)
                .ApplyConfigIfNotNull(
                    source,
                    sourceConfig => sourceConfig.ServerName,
                    (factory, value) => factory.ServerName = value)
                .ApplyConfigIfNotNull(
                    source,
                    sourceConfig => sourceConfig.Version,
                    (factory, value) => factory.Version = value);
        }

        private static TDestination ApplyConfigIfNotNull<TSource, TDestination, TValue>(
            this TDestination destination,
            TSource source,
            Func<TSource, TValue> sourceFunction,
            Action<TDestination, TValue> applyAction)
        {
            var value = sourceFunction(source);

            if (value != null)
                applyAction(destination, value);

            return destination;
        }

        private static TDestination ApplyConfigIfNotNull<TSource, TDestination, TValue>(
            this TDestination destination,
            TSource source,
            Func<TSource, TValue?> sourceFunction,
            Action<TDestination, TValue> applyAction)
            where TValue : struct
        {
            var value = sourceFunction(source);

            if (value.HasValue)
                applyAction(destination, value.Value);

            return destination;
        }
    }
}
