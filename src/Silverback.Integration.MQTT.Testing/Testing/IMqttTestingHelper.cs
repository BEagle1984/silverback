// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt.Mocks;

namespace Silverback.Testing
{
    /// <inheritdoc cref="ITestingHelper{TBroker}"/>
    public interface IMqttTestingHelper : ITestingHelper<MqttBroker>
    {
        /// <summary>
        ///     Gets the <see cref="IClientSession" /> of the specified client.
        /// </summary>
        /// <param name="clientId">
        ///     The client id.
        /// </param>
        /// <returns>
        ///     The <see cref="IClientSession" />.
        /// </returns>
        IClientSession GetClientSession(string clientId);

        /// <summary>
        ///     Gets the total number of messages that were published to the specified topic.
        /// </summary>
        /// <param name="topic">
        ///     The name of the topic.
        /// </param>
        /// <returns>
        ///     The number of messages published to the topic.
        /// </returns>
        int GetMessagesCount(string topic);
    }
}
