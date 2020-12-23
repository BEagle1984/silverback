// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddMockedMqtt</c> method to the <see cref="IBrokerOptionsBuilder" />.
    /// </summary>
    public static class BrokerOptionsBuilderAddMockedMqttExtensions
    {
        /// <summary>
        ///     Registers Apache Mqtt as message broker but replaces the MQTT connectivity based on MQTTnet
        ///     with a mocked in-memory message broker that <b>more or less</b> replicates the MQTT broker behavior.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddMockedMqtt(this IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.AddMqtt();
            brokerOptionsBuilder.SilverbackBuilder.UseMockedMqtt();

            return brokerOptionsBuilder;
        }
    }
}
