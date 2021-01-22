// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    internal class EndpointsConfiguratorsInvoker
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly object _lock = new();

        private bool _invoked;

        public EndpointsConfiguratorsInvoker(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public void Invoke()
        {
            lock (_lock)
            {
                if (_invoked)
                    return;

                _invoked = true;

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var endpointsConfigurationBuilder =
                        new EndpointsConfigurationBuilder(scope.ServiceProvider);

                    scope.ServiceProvider.GetServices<IEndpointsConfigurator>()
                        .ForEach(configurator => configurator.Configure(endpointsConfigurationBuilder));
                }
                catch (Exception)
                {
                    _invoked = false;
                    throw;
                }
            }
        }
    }
}
