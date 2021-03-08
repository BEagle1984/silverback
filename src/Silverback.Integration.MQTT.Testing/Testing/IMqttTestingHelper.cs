// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt.Mocks;

namespace Silverback.Testing
{
    /// <inheritdoc cref="ITestingHelper{TBroker}" />
    public interface IMqttTestingHelper : ITestingHelper<MqttBroker>
    {
        /// <summary>
        ///     Gets the <see cref="IClientSession" /> of the specified client.
        /// </summary>
        /// <remarks>
        ///     This method works with the mocked MQTT broker only. See
        ///     <see cref="SilverbackBuilderUseMockedMqttExtensions.UseMockedMqtt" /> or
        ///     <see cref="BrokerOptionsBuilderAddMockedMqttExtensions.AddMockedMqtt" />.
        /// </remarks>
        /// <param name="clientId">
        ///     The client id.
        /// </param>
        /// <returns>
        ///     The <see cref="IClientSession" />.
        /// </returns>
        IClientSession GetClientSession(string clientId);

        /// <summary>
        ///     Gets the messages that have been published to the specified topic.
        /// </summary>
        /// <remarks>
        ///     This method works with the mocked MQTT broker only. See
        ///     <see cref="SilverbackBuilderUseMockedMqttExtensions.UseMockedMqtt" /> or
        ///     <see cref="BrokerOptionsBuilderAddMockedMqttExtensions.AddMockedMqtt" />.
        /// </remarks>
        /// <param name="topic">
        ///     The name of the topic.
        /// </param>
        /// <returns>
        ///     The messages published to the topic.
        /// </returns>
        IReadOnlyList<MqttApplicationMessage> GetMessages(string topic);
    }
}
