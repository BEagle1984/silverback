// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the AddEndpointsConfigurator methods to the <see cref="SilverbackBuilder" />.
/// </content>
public static partial class SilverbackBuilderExtensions
{
        /// <summary>
        ///     Adds an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
        /// </param>
        /// <typeparam name="TConfigurator">
        ///     The type of the <see cref="IEndpointsConfigurator" /> to add.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static SilverbackBuilder AddEndpointsConfigurator<TConfigurator>(this SilverbackBuilder builder)
            where TConfigurator : class, IEndpointsConfigurator
        {
            Check.NotNull(builder, nameof(builder));

            builder.Services.AddScoped<IEndpointsConfigurator, TConfigurator>();

            return builder;
        }

        /// <summary>
        ///     Adds an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
        /// </param>
        /// <param name="configuratorType">
        ///     The type of the <see cref="IEndpointsConfigurator" /> to add.
        /// </param>
        /// <returns>
        ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static SilverbackBuilder AddEndpointsConfigurator(this SilverbackBuilder builder, Type configuratorType)
        {
            Check.NotNull(builder, nameof(builder));

            builder.Services.AddScoped(typeof(IEndpointsConfigurator), configuratorType);

            return builder;
        }

        /// <summary>
        ///     Adds an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the <see cref="IEndpointsConfigurator" /> to add.
        /// </param>
        /// <returns>
        ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static SilverbackBuilder AddEndpointsConfigurator(
            this SilverbackBuilder builder,
            Func<IServiceProvider, IEndpointsConfigurator> implementationFactory)
        {
            Check.NotNull(builder, nameof(builder));

            builder.Services.AddScoped(implementationFactory);

            return builder;
        }
    }
