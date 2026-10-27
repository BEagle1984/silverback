---
uid: sample-mqtt-basic
---

# MQTT - Basic

This sample implements the simplest possible producer and consumer.

## What This Sample Shows

- Connecting Silverback to an MQTT broker with `AddMqtt` and moving the clients configuration to an `IBrokerClientsConfigurator`.
- Producing the `SampleMessage` to the `samples/basic` topic with at-least-once QoS and the retain flag, logging a warning when no subscriber matches.
- Consuming the same topic with at-least-once QoS, skipping the messages that fail to be processed.
- Configuring a last will message (`TestamentMessage`) that is sent to the `samples/testaments` topic if the consumer disconnects ungracefully.

## How to Run

Start the infrastructure as described in <xref:samples>, then run the two applications from the repository root.

```bash
dotnet run --project ./samples/MQTT/Basic.Producer/.
dotnet run --project ./samples/MQTT/Basic.Consumer/.
```

Observe the console output while the messages are produced and consumed.

## Common

The message being exchanged is defined in a common project.

[!code-csharp[Common.Message](../../../samples/MQTT/Basic.Common/SampleMessage.cs)]

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/MQTT/Basic.Common_


## Producer

The producer uses a hosted service to publish some messages in the background.

# [Startup](#tab/producer-startup)
[!code-csharp[Producer.Startup](../../../samples/MQTT/Basic.Producer/Startup.cs)]
# [BrokerClientsConfigurator](#tab/producer-endpoints)
[!code-csharp[Producer.BrokerClientsConfigurator](../../../samples/MQTT/Basic.Producer/BrokerClientsConfigurator.cs)]
# [Background Service](#tab/producer-background-service)
[!code-csharp[Producer.BackgroundService](../../../samples/MQTT/Basic.Producer/ProducerBackgroundService.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/MQTT/Basic.Producer_

## Consumer

The consumer processes the messages and outputs their value to the standard output.

# [Startup](#tab/consumer-startup)
[!code-csharp[Consumer.Startup](../../../samples/MQTT/Basic.Consumer/Startup.cs)]
# [BrokerClientsConfigurator](#tab/consumer-endpoints)
[!code-csharp[Consumer.BrokerClientsConfigurator](../../../samples/MQTT/Basic.Consumer/BrokerClientsConfigurator.cs)]
# [Subscriber](#tab/consumer-subscriber)
[!code-csharp[Consumer.Subscriber](../../../samples/MQTT/Basic.Consumer/SampleMessageSubscriber.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/MQTT/Basic.Consumer_

## Additional Resources

- <xref:broker>
- <xref:producing>
- <xref:consuming>
- <xref:sample-mqtt-basic-v3> sample
