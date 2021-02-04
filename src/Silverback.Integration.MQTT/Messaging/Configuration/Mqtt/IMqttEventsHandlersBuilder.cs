using System;
using System.Threading.Tasks;
using MQTTnet.Client;

namespace Silverback.Messaging.Configuration.Mqtt
{
    /// <summary>
    ///     Builds the <see cref="MqttEventsHandlers"/>.
    /// </summary>
    public interface IMqttEventsHandlersBuilder
    {
        /// <summary>
        ///     Registers an event handler that is called when the underlying <see cref="IMqttClient"/>
        ///     connected to the broker.
        /// </summary>
        /// <param name="connectedHandler">
        ///     The event handler.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttEventsHandlersBuilder"/> so that additional calls can be chained.
        /// </returns>
        IMqttEventsHandlersBuilder OnConnected(Func<MqttClientConfig, Task> connectedHandler);

        /// <summary>
        ///     Registers an event handler that is called before the underlying <see cref="IMqttClient"/>
        ///     disconnects from the broker.
        /// </summary>
        /// <param name="disconnectingHandler">
        ///     The event handler.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttEventsHandlersBuilder"/> so that additional calls can be chained.
        /// </returns>
        IMqttEventsHandlersBuilder OnDisconnecting(Func<MqttClientConfig, Task> disconnectingHandler);
    }
}
