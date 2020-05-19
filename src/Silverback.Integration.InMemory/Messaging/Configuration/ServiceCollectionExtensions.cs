// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Messaging.Broker;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>OverrideWithInMemoryBroker</c> method to the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Registers the fake in-memory message broker, replacing any previously registered <see cref="IBroker" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection"/> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection OverrideWithInMemoryBroker(
            this IServiceCollection services)
        {
            services.RemoveAll<IBroker>();
            services.AddSingleton<IBroker, InMemoryBroker>();

            return services;
        }
    }
}