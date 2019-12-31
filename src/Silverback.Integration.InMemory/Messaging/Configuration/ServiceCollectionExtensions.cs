// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Messaging.Broker;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Registers the fake in-memory message broker,
        ///     replacing any previously registered <see cref="IBroker" />.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection OverrideWithInMemoryBroker(
            this IServiceCollection services)
        {
            services.RemoveAll<IBroker>();
            services.AddSingleton<IBroker, InMemoryBroker>();

            return services;
        }
    }
}