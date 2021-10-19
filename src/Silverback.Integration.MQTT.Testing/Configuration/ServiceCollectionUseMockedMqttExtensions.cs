// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Configuration;

/// <summary>
///     Adds the <c>UseMockedMqtt</c> method to the <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionUseMockedMqttExtensions
{
    /// <summary>
    ///     Replaces the MQTT connectivity based on MQTTnet with a mocked in-memory message broker that
    ///     <b>more or less</b> replicates the MQTT broker behavior.
    /// </summary>
    /// <param name="services">
    ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
    ///     to.
    /// </param>
    /// <returns>
    ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection UseMockedMqtt(this IServiceCollection services)
    {
        services.ConfigureSilverback().UseMockedMqtt();
        return services;
    }
}
