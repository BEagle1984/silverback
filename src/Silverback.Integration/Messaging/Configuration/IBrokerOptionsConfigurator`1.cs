// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     The implementations of this class will be located via assembly scanning and invoked when a broker of the matching type
///     <typeparamref name="TBroker" /> is added to the <see cref="IServiceCollection" />.
/// </summary>
/// <typeparam name="TBroker">
///     The type of the <see cref="IBroker" /> implementation being configured.
/// </typeparam>
[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used to find the configurator for the broker type being added")]
public interface IBrokerOptionsConfigurator<TBroker>
    where TBroker : IBroker
{
    /// <summary>
    ///     Called while registering the broker to configure the broker-specific services and options (e.g. behaviors).
    /// </summary>
    /// <param name="brokerOptionsBuilder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    void Configure(BrokerOptionsBuilder brokerOptionsBuilder);
}
