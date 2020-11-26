// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.MqttNetWrappers;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     This class will be located via assembly scanning and invoked when a <see cref="MqttBroker" /> is
    ///     added to the <see cref="IServiceCollection" />.
    /// </summary>
    public class MqttBrokerOptionsConfigurator : IBrokerOptionsConfigurator<MqttBroker>
    {
        /// <inheritdoc cref="IBrokerOptionsConfigurator{TBroker}.Configure" />
        public void Configure(IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder
                .Services
                .AddSingleton<IMqttClientsCache, MqttClientsCache>()
                .AddSingleton<IMqttNetClientFactory, MqttNetClientFactory>();
        }
    }
}
