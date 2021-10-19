// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <c>AddMqtt</c> method to the <see cref="IBrokerOptionsBuilder" />.
/// </summary>
public static class BrokerOptionsBuilderAddMqttExtensions
{
    /// <summary>
    ///     Registers Apache Mqtt as message broker.
    /// </summary>
    /// <param name="brokerOptionsBuilder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder AddMqtt(this BrokerOptionsBuilder brokerOptionsBuilder) =>
        Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder)).AddBroker<MqttBroker>();
}
