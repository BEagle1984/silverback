// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

internal sealed class BrokerClientsBootstrapper
{
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly IBrokerClientCallbacksInvoker _callbackInvoker;

    private readonly ISilverbackLogger<BrokerClientsConfigurationBuilder> _logger;

    public BrokerClientsBootstrapper(
        IServiceScopeFactory scopeFactory,
        IBrokerClientCallbacksInvoker callbackInvoker,
        ISilverbackLogger<BrokerClientsConfigurationBuilder> logger)
    {
        _scopeFactory = Check.NotNull(scopeFactory, nameof(scopeFactory));
        _callbackInvoker = Check.NotNull(callbackInvoker, nameof(callbackInvoker));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    public async ValueTask InitializeAllAsync()
    {
        using IServiceScope scope = _scopeFactory.CreateScope();

        InvokeConfigurators(scope);
        InvokeClientsInitializers(scope);
        await InvokeCallbacksAsync().ConfigureAwait(false);
    }

    private static void InvokeClientsInitializers(IServiceScope scope)
    {
        foreach (IBrokerClientsInitializer initializer in scope.ServiceProvider.GetServices<IBrokerClientsInitializer>())
        {
            initializer.Initialize();
        }
    }

    private void InvokeConfigurators(IServiceScope scope)
    {
        BrokerClientsConfigurationBuilder brokerClientsConfigurationBuilder = new(scope.ServiceProvider);

        foreach (IBrokerClientsConfigurator? configurator in scope.ServiceProvider.GetServices<IBrokerClientsConfigurator>())
        {
            InvokeConfigurator(configurator, brokerClientsConfigurationBuilder);
        }
    }

    [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
    private void InvokeConfigurator(
        IBrokerClientsConfigurator configurator,
        BrokerClientsConfigurationBuilder brokerClientsConfigurationBuilder)
    {
        try
        {
            configurator.Configure(brokerClientsConfigurationBuilder);
        }
        catch (Exception ex)
        {
            _logger.LogEndpointConfiguratorError(configurator, ex);
        }
    }

    private async ValueTask InvokeCallbacksAsync()
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        await _callbackInvoker.InvokeAsync<IBrokerClientsConfiguredCallback>(
                handler => handler.OnBrokerClientsConfiguredAsync(),
                scope.ServiceProvider)
            .ConfigureAwait(false);
    }
}
