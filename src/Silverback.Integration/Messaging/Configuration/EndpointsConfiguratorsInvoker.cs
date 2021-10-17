// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    internal sealed class EndpointsConfiguratorsInvoker
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly IBrokerCallbacksInvoker _callbackInvoker;

        private readonly ISilverbackLogger<EndpointsConfigurationBuilder> _logger;

        private readonly object _lock = new();

        private Task? _invokeTask;

        public EndpointsConfiguratorsInvoker(
            IServiceScopeFactory scopeFactory,
            IBrokerCallbacksInvoker callbackInvoker,
            ISilverbackLogger<EndpointsConfigurationBuilder> logger)
        {
            _scopeFactory = Check.NotNull(scopeFactory, nameof(scopeFactory));
            _callbackInvoker = Check.NotNull(callbackInvoker, nameof(callbackInvoker));
            _logger = Check.NotNull(logger, nameof(logger));
        }

        public Task InvokeAsync()
        {
            lock (_lock)
            {
                _invokeTask ??= InvokeCoreAsync();
            }

            return _invokeTask;
        }

        private async Task InvokeCoreAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var endpointsConfigurationBuilder = new EndpointsConfigurationBuilder(scope.ServiceProvider);

            foreach (var configurator in scope.ServiceProvider.GetServices<IEndpointsConfigurator>())
            {
                InvokeConfigurator(configurator, endpointsConfigurationBuilder);
            }

            await _callbackInvoker.InvokeAsync<IEndpointsConfiguredCallback>(
                    handler => handler.OnEndpointsConfiguredAsync(),
                    scope.ServiceProvider)
                .ConfigureAwait(false);
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
