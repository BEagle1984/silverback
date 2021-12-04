// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Background;
using Silverback.Util;

namespace Silverback.Tests;

public static class HostedServicesControlServiceProviderExtensions
{
    public static void PauseSilverbackBackgroundServices(this IServiceProvider serviceProvider) =>
        GetSilverbackServicesHostedServices(serviceProvider)
            .ForEach(service => service.Pause());

    public static void ResumeSilverbackBackgroundServices(this IServiceProvider serviceProvider) =>
        GetSilverbackServicesHostedServices(serviceProvider)
            .ForEach(service => service.Resume());

    private static IEnumerable<RecurringDistributedBackgroundService> GetSilverbackServicesHostedServices(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetServices<IHostedService>()
            .OfType<RecurringDistributedBackgroundService>()
            .Where(
                service =>
                    service.GetType().Namespace != null &&
                    service.GetType().Namespace!.StartsWith("Silverback", StringComparison.Ordinal));
    }
}
