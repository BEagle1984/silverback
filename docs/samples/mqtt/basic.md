---
uid: sample-mqtt-basic
---

# MQTT - Basic

This sample implements the simple possible producer and consumer.

See also: <xref:message-broker>

## Common

The message being exchanged is defined in a common project.

[!code-csharp[Common.Message](../../../samples/Mqtt/Basic.Common/SampleMessage.cs)]

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Mqtt/Basic.Common_


## Producer

The producer uses a hosted service to publish some messages in the background.

# [Startup](#tab/producer-startup)
[!code-csharp[Producer.Startup](../../../samples/Mqtt/Basic.Producer/Startup.cs)]
# [EndpointsConfigurator](#tab/producer-endpoints)
[!code-csharp[Producer.EndpointsConfigurator](../../../samples/Mqtt/Basic.Producer/EndpointsConfigurator.cs)]
# [Background Service](#tab/producer-background-service)
[!code-csharp[Producer.BackgroundService](../../../samples/Mqtt/Basic.Producer/ProducerBackgroundService.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Mqtt/Basic.Producer_

## Consumer

The consumer processes the messages and outputs their value to the standard output.

# [Startup](#tab/consumer-startup)
[!code-csharp[Consumer.Startup](../../../samples/Mqtt/Basic.Consumer/Startup.cs)]
# [EndpointsConfigurator](#tab/consumer-endpoints)
[!code-csharp[Consumer.EndpointsConfigurator](../../../samples/Mqtt/Basic.Consumer/EndpointsConfigurator.cs)]
# [Subscriber](#tab/consumer-subscriber)
[!code-csharp[Consumer.Subscriber](../../../samples/Mqtt/Basic.Consumer/SampleMessageSubscriber.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Mqtt/Basic.Consumer_
