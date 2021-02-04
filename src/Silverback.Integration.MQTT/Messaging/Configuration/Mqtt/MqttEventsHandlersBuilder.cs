using System;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt
{
    internal class MqttEventsHandlersBuilder : IMqttEventsHandlersBuilder
    {
        private Func<MqttClientConfig, Task>? _connectedHandler;

        private Func<MqttClientConfig, Task>? _disconnectingHandler;

        public MqttEventsHandlersBuilder()
        {
        }

        public MqttEventsHandlersBuilder(MqttEventsHandlers mqttEventsHandlers)
        {
            Check.NotNull(mqttEventsHandlers, nameof(mqttEventsHandlers));

            _connectedHandler =
                (Func<MqttClientConfig, Task>?)mqttEventsHandlers.ConnectedHandler?.Clone();
            _disconnectingHandler =
                (Func<MqttClientConfig, Task>?)mqttEventsHandlers.DisconnectingHandler?.Clone();
        }

        public IMqttEventsHandlersBuilder OnConnected(Func<MqttClientConfig, Task> connectedHandler)
        {
            Check.NotNull(connectedHandler, nameof(connectedHandler));
            _connectedHandler = connectedHandler;
            return this;
        }

        public IMqttEventsHandlersBuilder OnDisconnecting(Func<MqttClientConfig, Task> disconnectingHandler)
        {
            Check.NotNull(disconnectingHandler, nameof(disconnectingHandler));
            _disconnectingHandler = disconnectingHandler;
            return this;
        }

        public MqttEventsHandlers Build() =>
            new()
            {
                ConnectedHandler = _connectedHandler,
                DisconnectingHandler = _disconnectingHandler
            };
    }
}
