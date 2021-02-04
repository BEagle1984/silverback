using System;
using System.Threading.Tasks;
using MQTTnet.Client;

namespace Silverback.Messaging.Configuration.Mqtt
{
    /// <summary>
    ///     Defines the handlers for the Mqtt Client events such as connected and disconnecting.
    /// </summary>
    public class MqttEventsHandlers
    {
        /// <summary>
        ///     Gets or sets the handler to call when the underlying <see cref="IMqttClient"/>
        ///     connected to the broker.
        /// </summary>
        public Func<MqttClientConfig, Task>? ConnectedHandler { get; set; }

        /// <summary>
        ///     Gets or sets the handler to call befor the underlying <see cref="IMqttClient"/>
        ///     disconnects from the broker.
        /// </summary>
        public Func<MqttClientConfig, Task>? DisconnectingHandler { get; set; }
    }
}
