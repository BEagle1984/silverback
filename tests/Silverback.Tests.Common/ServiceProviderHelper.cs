// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Silverback.Tests;

public static class ServiceProviderHelper
{
    public static IServiceProvider GetScopedServiceProvider(
        Action<IServiceCollection> servicesConfigurationAction,
        IHostApplicationLifetime? hostApplicationLifetime = null) =>
        GetServiceProvider(servicesConfigurationAction, hostApplicationLifetime).CreateScope().ServiceProvider;

    public static IServiceProvider GetServiceProvider(
        Action<IServiceCollection> servicesConfigurationAction,
        IHostApplicationLifetime? hostApplicationLifetime = null)
    {
        ServiceCollection services = [];
        services.AddSingleton(hostApplicationLifetime ?? Substitute.For<IHostApplicationLifetime>());

        servicesConfigurationAction(services);

        ServiceProviderOptions options = new() { ValidateScopes = true };
        return services.BuildServiceProvider(options);
    }
}
