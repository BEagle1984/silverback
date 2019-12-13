// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        #region RegisterConfigurator

        /// <summary>
        /// Adds an <see cref="IEndpointsConfigurator"/> to be used
        /// to setup the broker endpoints.
        /// </summary>
        /// <typeparam name="TConfigurator">The type of the <see cref="IEndpointsConfigurator"/> to add.</typeparam>
        /// <returns></returns>
        public static IServiceCollection AddEndpointsConfigurator<TConfigurator>(this  IServiceCollection services) 
            where TConfigurator : class, IEndpointsConfigurator =>
            services.AddTransient<IEndpointsConfigurator, TConfigurator>();

        #endregion
    }
}