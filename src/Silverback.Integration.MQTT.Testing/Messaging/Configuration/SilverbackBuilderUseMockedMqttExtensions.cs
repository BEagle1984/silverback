// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Broker.Mqtt.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Testing;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>UseMockedMqtt</c> method to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderUseMockedMqttExtensions
    {
        /// <summary>
        ///     Replaces the MQTT connectivity based on MQTTnet with a mocked in-memory message broker that
        ///     <b>more or less</b> replicates the MQTT broker behavior.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="ISilverbackBuilder" />.
        /// </param>
        /// <param name="optionsAction">
        ///     Configures the mock options.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder UseMockedMqtt(
            this ISilverbackBuilder builder,
            Action<IMockedMqttOptionsBuilder>? optionsAction = null)
        {
            Check.NotNull(builder, nameof(builder));

            builder.Services
                .RemoveAll<IMqttNetClientFactory>()
                .AddSingleton<IMqttNetClientFactory, MockedMqttNetClientFactory>()
                .AddSingleton<IMockedMqttOptions>(new MockedMqttOptions())
                .AddSingleton<IInMemoryMqttBroker, InMemoryMqttBroker>()
                .AddSingleton<IMqttTestingHelper, MqttTestingHelper>();

            var optionsBuilder = new MockedMqttOptionsBuilder(builder.Services);
            optionsAction?.Invoke(optionsBuilder);

            return builder;
        }
    }
}
