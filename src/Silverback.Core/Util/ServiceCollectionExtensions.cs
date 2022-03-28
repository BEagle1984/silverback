// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Util;

internal static class ServiceCollectionExtensions
{
    public static bool ContainsAny<TService>(this IServiceCollection services) =>
        services.ContainsAny(typeof(TService));

    public static bool ContainsAny(this IServiceCollection services, Type serviceType) =>
        services.Any(descriptor => descriptor.ServiceType == serviceType);

    public static TService? GetSingletonServiceInstance<TService>(this IServiceCollection services) =>
        (TService?)services.GetSingletonServiceInstance(typeof(TService)) ?? default;

    public static object? GetSingletonServiceInstance(this IServiceCollection services, Type serviceType) =>
        services.FirstOrDefault(descriptor => descriptor.ServiceType == serviceType)?.ImplementationInstance;

    public static IReadOnlyList<ServiceDescriptor> GetAll<TService>(this IServiceCollection services) =>
        services.GetAll(typeof(TService));

    public static IReadOnlyList<ServiceDescriptor> GetAll(this IServiceCollection services, Type serviceType) =>
        services.Where(descriptor => descriptor.ServiceType == serviceType).ToList();
}
