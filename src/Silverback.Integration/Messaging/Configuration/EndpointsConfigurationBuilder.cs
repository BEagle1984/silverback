// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Configuration
{
    /// <inheritdoc />
    public class EndpointsConfigurationBuilder : IEndpointsConfigurationBuilder
    {
        private readonly IOutboundRoutingConfiguration _outboundRoutingConfiguration;
        private readonly IReadOnlyCollection<IInboundConnector> _inboundConnectors;
        private readonly ErrorPolicyBuilder _errorPolicyBuilder;
        private readonly IServiceProvider _serviceProvider;

        public EndpointsConfigurationBuilder(
            IOutboundRoutingConfiguration outboundRoutingConfiguration,
            IEnumerable<IInboundConnector> inboundConnectors,
            ErrorPolicyBuilder errorPolicyBuilder,
            IServiceProvider serviceProvider)
        {
            _outboundRoutingConfiguration = outboundRoutingConfiguration;
            _inboundConnectors = inboundConnectors.ToList();
            _errorPolicyBuilder = errorPolicyBuilder;
            _serviceProvider = serviceProvider;
        }

        public IEndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(params IProducerEndpoint[] endpoints)
            where TConnector : IOutboundConnector =>
            AddOutbound(typeof(TMessage), endpoints, typeof(TConnector));

        public IEndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(
            IEnumerable<IProducerEndpoint> endpoints)
            where TConnector : IOutboundConnector =>
            AddOutbound(typeof(TMessage), endpoints, typeof(TConnector));

        public IEndpointsConfigurationBuilder AddOutbound<TMessage>(params IProducerEndpoint[] endpoints) =>
            AddOutbound(typeof(TMessage), endpoints, null);

        public IEndpointsConfigurationBuilder AddOutbound<TMessage>(
            IEnumerable<IProducerEndpoint> endpoints,
            Type outboundConnectorType = null) =>
            AddOutbound(typeof(TMessage), endpoints, outboundConnectorType);

        public IEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            IEnumerable<IProducerEndpoint> endpoints,
            Type outboundConnectorType) =>
            AddOutbound(messageType, new StaticOutboundRouter(endpoints), outboundConnectorType);

        public IEndpointsConfigurationBuilder AddOutbound<TMessage, TRouter, TConnector>()
            where TRouter : IOutboundRouter<TMessage>
            where TConnector : IOutboundConnector =>
            AddOutbound(typeof(TMessage), typeof(TRouter), typeof(TConnector));

        public IEndpointsConfigurationBuilder AddOutbound<TMessage, TRouter>()
            where TRouter : IOutboundRouter<TMessage> =>
            AddOutbound(typeof(TMessage), typeof(TRouter), null);

        public IEndpointsConfigurationBuilder AddOutbound<TMessage, TConnector>(IOutboundRouter<TMessage> router)
            where TConnector : IOutboundConnector =>
            AddOutbound(typeof(TMessage), router, typeof(TConnector));

        public IEndpointsConfigurationBuilder AddOutbound<TMessage>(
            IOutboundRouter<TMessage> router,
            Type outboundConnectorType = null) =>
            AddOutbound(typeof(TMessage), router, outboundConnectorType);

        public IEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            Type routerType,
            Type outboundConnectorType)
        {
            var router = (IOutboundRouter) _serviceProvider.GetRequiredService(routerType);
            _outboundRoutingConfiguration.Add(messageType, router, outboundConnectorType);
            return this;
        }

        public IEndpointsConfigurationBuilder AddOutbound(
            Type messageType,
            IOutboundRouter router,
            Type outboundConnectorType)
        {
            _outboundRoutingConfiguration.Add(messageType, router, outboundConnectorType);
            return this;
        }

        public IEndpointsConfigurationBuilder AddInbound(
            IConsumerEndpoint endpoint,
            Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null,
            InboundConnectorSettings settings = null)
        {
            AddInbound(endpoint, null, errorPolicyFactory, settings);
            return this;
        }

        public IEndpointsConfigurationBuilder AddInbound<TConnector>(
            IConsumerEndpoint endpoint,
            Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null,
            InboundConnectorSettings settings = null)
            where TConnector : IInboundConnector
        {
            AddInbound(endpoint, typeof(TConnector), errorPolicyFactory, settings);
            return this;
        }

        public IEndpointsConfigurationBuilder AddInbound(
            IConsumerEndpoint endpoint,
            Type inboundConnectorType,
            Func<ErrorPolicyBuilder, IErrorPolicy> errorPolicyFactory = null,
            InboundConnectorSettings settings = null)
        {
            _inboundConnectors.GetConnectorInstance(inboundConnectorType).Bind(endpoint,
                errorPolicyFactory?.Invoke(_errorPolicyBuilder), settings);
            return this;
        }

        public IEndpointsConfigurationBuilder PublishOutboundMessagesToInternalBus()
        {
            _outboundRoutingConfiguration.PublishOutboundMessagesToInternalBus = true;
            return this;
        }
    }
}