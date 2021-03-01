---
uid: sample-mqtt-basic-v3
---

# MQTT - Basic (v3)

This sample implements the simple possible producer and consumer, but using MQTT protocol version 3.

See also: <xref:message-broker>

## Common

The message being exchanged is defined in a common project.

[!code-csharp[Common.Message](../../../samples/Mqtt/Basic.Common/SampleMessage.cs)]

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Mqtt/Basic.Common_


## Producer

The producer uses an hosted service to publish some messages in the background.

# [Startup](#tab/producer-startup)
[!code-csharp[Producer.Startup](../../../samples/Mqtt/Basic.ProducerV3/Startup.cs)]
# [EndpointsConfigurator](#tab/producer-endpoints)
[!code-csharp[Producer.EndpointsConfigurator](../../../samples/Mqtt/Basic.ProducerV3/EndpointsConfigurator.cs)]
# [Background Service](#tab/producer-background-service)
[!code-csharp[Producer.BackgroundService](../../../samples/Mqtt/Basic.ProducerV3/ProducerBackgroundService.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Mqtt/Basic.ProducerV3_

## Consumer

The consumer simply streams the file to a temporary folder in the local file system.

# [Startup](#tab/consumer-startup)
[!code-csharp[Consumer.Startup](../../../samples/Mqtt/Basic.ConsumerV3/Startup.cs)]
# [EndpointsConfigurator](#tab/consumer-endpoints)
[!code-csharp[Consumer.EndpointsConfigurator](../../../samples/Mqtt/Basic.ConsumerV3/EndpointsConfigurator.cs)]
# [Subscriber](#tab/consumer-subscriber)
[!code-csharp[Consumer.Subscriber](../../../samples/Mqtt/Basic.ConsumerV3/SampleMessageSubscriber.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Mqtt/Basic.ConsumerV3_
