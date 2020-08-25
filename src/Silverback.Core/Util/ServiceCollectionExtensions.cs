// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Util
{
    internal static class ServiceCollectionExtensions
    {
        public static bool ContainsAny<TService>(this IServiceCollection services) =>
            services.ContainsAny(typeof(TService));

        public static bool ContainsAny(this IServiceCollection services, Type serviceType) =>
            services.Any(service => service.ServiceType == serviceType);

        [return: MaybeNull]
        public static TService GetSingletonServiceInstance<TService>(this IServiceCollection services)
        {
            var instance = services.GetSingletonServiceInstance(typeof(TService));

            return instance != null ? (TService)instance : default;
        }

        public static object? GetSingletonServiceInstance(this IServiceCollection services, Type serviceType) =>
            services.FirstOrDefault(service => service.ServiceType == serviceType)?.ImplementationInstance;
    }
}
