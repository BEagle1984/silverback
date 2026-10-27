---
uid: sample-mqtt-basic-v3
---

# MQTT - Basic (MQTT v3)

This sample implements the simplest possible producer and consumer, but using MQTT protocol version 3.

## What This Sample Shows

- Selecting the MQTT protocol version 3.1.0 with `UseProtocolVersion(MqttProtocolVersion.V310)`.
- Producing the `SampleMessage` to the `samples/basic` topic with at-least-once QoS and the retain flag.
- Consuming the same topic with at-least-once QoS, skipping the messages that fail to be processed.
- Configuring a last will message (`TestamentMessage`) that is sent to the `samples/testaments` topic if the consumer disconnects ungracefully.

## How to Run

Start the infrastructure as described in <xref:samples>, then run the two applications from the repository root.

```bash
dotnet run --project ./samples/MQTT/Basic.ProducerV3/.
dotnet run --project ./samples/MQTT/Basic.ConsumerV3/.
```

Observe the console output while the messages are produced and consumed.

## Common

The message being exchanged is defined in a common project.

[!code-csharp[Common.Message](../../../samples/MQTT/Basic.Common/SampleMessage.cs)]

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/MQTT/Basic.Common_


## Producer

The producer uses a hosted service to publish some messages in the background.

# [Startup](#tab/producer-startup)
[!code-csharp[Producer.Startup](../../../samples/MQTT/Basic.ProducerV3/Startup.cs)]
# [BrokerClientsConfigurator](#tab/producer-endpoints)
[!code-csharp[Producer.BrokerClientsConfigurator](../../../samples/MQTT/Basic.ProducerV3/BrokerClientsConfigurator.cs)]
# [Background Service](#tab/producer-background-service)
[!code-csharp[Producer.BackgroundService](../../../samples/MQTT/Basic.ProducerV3/ProducerBackgroundService.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/MQTT/Basic.ProducerV3_

## Consumer

The consumer processes the messages and outputs their value to the standard output.

# [Startup](#tab/consumer-startup)
[!code-csharp[Consumer.Startup](../../../samples/MQTT/Basic.ConsumerV3/Startup.cs)]
# [BrokerClientsConfigurator](#tab/consumer-endpoints)
[!code-csharp[Consumer.BrokerClientsConfigurator](../../../samples/MQTT/Basic.ConsumerV3/BrokerClientsConfigurator.cs)]
# [Subscriber](#tab/consumer-subscriber)
[!code-csharp[Consumer.Subscriber](../../../samples/MQTT/Basic.ConsumerV3/SampleMessageSubscriber.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/MQTT/Basic.ConsumerV3_

## Additional Resources

- <xref:broker>
- <xref:producing>
- <xref:consuming>
- <xref:sample-mqtt-basic> sample
