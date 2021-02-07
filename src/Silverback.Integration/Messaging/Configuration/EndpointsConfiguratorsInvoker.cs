// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    internal class EndpointsConfiguratorsInvoker
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ISilverbackLogger<EndpointsConfigurationBuilder> _logger;

        private readonly object _lock = new();

        private bool _invoked;

        public EndpointsConfiguratorsInvoker(
            IServiceScopeFactory scopeFactory,
            ISilverbackLogger<EndpointsConfigurationBuilder> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public void Invoke()
        {
            lock (_lock)
            {
                if (_invoked)
                    return;

                _invoked = true;

                using var scope = _scopeFactory.CreateScope();
                var endpointsConfigurationBuilder =
                    new EndpointsConfigurationBuilder(scope.ServiceProvider);

                scope.ServiceProvider.GetServices<IEndpointsConfigurator>()
                    .ForEach(configurator => InvokeConfigurator(configurator, endpointsConfigurationBuilder));
            }
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private void InvokeConfigurator(
            IEndpointsConfigurator configurator,
            EndpointsConfigurationBuilder endpointsConfigurationBuilder)
        {
            try
            {
                configurator.Configure(endpointsConfigurationBuilder);
            }
            catch (Exception ex)
            {
                _logger.LogEndpointConfiguratorError(configurator, ex);
            }
        }
    }
}
