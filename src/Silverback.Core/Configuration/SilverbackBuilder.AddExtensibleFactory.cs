﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.ExtensibleFactories;

namespace Silverback.Configuration;

/// <content>
///     Adds the <see cref="AddExtensibleFactory{TFactoryInterface,TFactory}" /> method to the <see cref="SilverbackBuilder" />.
/// </content>
public partial class SilverbackBuilder
{
    /// <summary>
    ///     Adds the <see cref="ExtensibleFactory{TService,TSettingsBase}" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    internal SilverbackBuilder AddExtensibleFactory<TFactoryInterface, TFactory>()
        where TFactoryInterface : class
        where TFactory : class, TFactoryInterface, IExtensibleFactory, new()
    {
        Services.AddSingleton<TFactoryInterface, TFactory>(serviceProvider => serviceProvider.GetService<TFactory>())
            .AddSingleton(new TFactory());

        return this;
    }

    /// <summary>
    ///     Adds the <see cref="ExtensibleFactory{TService,TSettingsBase}" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    internal SilverbackBuilder AddTypeBasedExtensibleFactory<TFactoryInterface, TFactory>()
        where TFactoryInterface : class
        where TFactory : class, TFactoryInterface, ITypeBasedExtensibleFactory, new()
    {
        Services.AddSingleton<TFactoryInterface, TFactory>(serviceProvider => serviceProvider.GetService<TFactory>())
            .AddSingleton(new TFactory());

        return this;
    }
}
