// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c> AddOutbound </c> method to the <see cref="IEndpointsConfigurationBuilder" />.
    /// </summary>
    public static class EndpointsConfigurationBuilderAddInboundExtensions
    {
        /// <summary> Adds and inbound endpoint. </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="endpoint"> The endpoint (topic). </param>
        /// <param name="errorPolicyFactory">
        ///     An optional function returning the error policy to be applied in case of exceptions while consuming
        ///     the messages from this topic.
        /// </param>
        /// <param name="settings">
        ///     The optional additional settings. If not specified, the default settings will be used.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddInbound(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            IConsumerEndpoint endpoint,
            Func<IErrorPolicyBuilder, IErrorPolicy>? errorPolicyFactory = null,
            InboundConnectorSettings? settings = null)
        {
            if (endpointsConfigurationBuilder == null)
                throw new ArgumentNullException(nameof(endpointsConfigurationBuilder));

            endpointsConfigurationBuilder.AddInbound(endpoint, null, errorPolicyFactory, settings);

            return endpointsConfigurationBuilder;
        }

        /// <summary> Adds and inbound endpoint. </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="endpoint"> The endpoint (topic). </param>
        /// <param name="errorPolicyFactory">
        ///     An optional function returning the error policy to be applied in case of exceptions while consuming
        ///     the messages from this topic.
        /// </param>
        /// <param name="settings">
        ///     The optional additional settings. If not specified, the default settings will be used.
        /// </param>
        /// <typeparam name="TConnector">
        ///     The type of the <see cref="IInboundConnector" /> to be used.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddInbound<TConnector>(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            IConsumerEndpoint endpoint,
            Func<IErrorPolicyBuilder, IErrorPolicy>? errorPolicyFactory = null,
            InboundConnectorSettings? settings = null)
            where TConnector : IInboundConnector
        {
            if (endpointsConfigurationBuilder == null)
                throw new ArgumentNullException(nameof(endpointsConfigurationBuilder));

            endpointsConfigurationBuilder.AddInbound(endpoint, typeof(TConnector), errorPolicyFactory, settings);

            return endpointsConfigurationBuilder;
        }

        /// <summary> Adds and inbound endpoint. </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="endpoint"> The endpoint (topic). </param>
        /// <param name="inboundConnectorType">
        ///     The type of the <see cref="IInboundConnector" /> to be used. If not specified, the default one will
        ///     be used.
        /// </param>
        /// <param name="errorPolicyFactory">
        ///     An optional function returning the error policy to be applied in case of exceptions while consuming
        ///     the messages from this topic.
        /// </param>
        /// <param name="settings">
        ///     The optional additional settings. If not specified, the default settings will be used.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddInbound(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            IConsumerEndpoint endpoint,
            Type? inboundConnectorType,
            Func<IErrorPolicyBuilder, IErrorPolicy>? errorPolicyFactory = null,
            InboundConnectorSettings? settings = null)
        {
            if (endpointsConfigurationBuilder == null)
                throw new ArgumentNullException(nameof(endpointsConfigurationBuilder));

            var inboundConnectors = endpointsConfigurationBuilder.ServiceProvider
                .GetServices<IInboundConnector>().ToList();

            var errorPolicyBuilder = endpointsConfigurationBuilder.ServiceProvider
                .GetRequiredService<IErrorPolicyBuilder>();

            inboundConnectors.GetConnectorInstance(inboundConnectorType).Bind(
                endpoint,
                errorPolicyFactory?.Invoke(errorPolicyBuilder),
                settings);

            return endpointsConfigurationBuilder;
        }
    }
}
