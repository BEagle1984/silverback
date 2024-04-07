// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Broker.Mqtt.Mocks;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Testing;
using Silverback.Util;

namespace Silverback.Configuration;

/// <summary>
///     Adds the <c>UseMockedMqtt</c> method to the <see cref="SilverbackBuilder" />.
/// </summary>
public static class SilverbackBuilderMqttTestingExtensions
{
    /// <summary>
    ///     Replaces the MQTT connectivity based on MQTTnet with a mocked in-memory message broker that
    ///     <b>more or less</b> replicates the MQTT broker behavior.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" />.
    /// </param>
    /// <param name="optionsAction">
    ///     Configures the mock options.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder UseMockedMqtt(
        this SilverbackBuilder builder,
        Action<IMockedMqttOptionsBuilder>? optionsAction = null)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services
            .RemoveAll<IMqttNetClientFactory>()
            .AddSingleton<IMqttNetClientFactory, MockedMqttNetClientFactory>()
            .AddSingleton<IMockedMqttOptions>(new MockedMqttOptions())
            .AddSingleton<IInMemoryMqttBroker, InMemoryMqttBroker>()
            .AddSingleton<IMqttTestingHelper, MqttTestingHelper>();

        MockedMqttOptionsBuilder optionsBuilder = new(builder.Services);
        optionsAction?.Invoke(optionsBuilder);

        return builder;
    }
}
