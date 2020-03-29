// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Util
{
    internal static class ServiceCollectionExtensions
    {
        public static bool ContainsAny<TService>(this IServiceCollection services) =>
            services.ContainsAny(typeof(TService));

        public static bool ContainsAny(this IServiceCollection services, Type serviceType) =>
            services.Any(s => s.ServiceType == serviceType);
    }
}