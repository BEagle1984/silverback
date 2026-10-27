---
uid: sample-kafka-basic
---

# Kafka - Basic

This sample implements the simplest possible producer and consumer.

## What This Sample Shows

- Connecting Silverback to Kafka with `AddKafka` and moving the broker clients configuration to an `IBrokerClientsConfigurator`.
- Configuring a producer that routes the `SampleMessage` to the `samples-basic` topic and publishing it through the `IPublisher` from a hosted service (one message every 100 ms).
- Configuring a consumer with a group id, starting from the earliest offset when no offset is stored for the group.
- Applying an error policy that retries each message twice and then skips it.
- Subscribing to the `IInboundEnvelope<SampleMessage>` to access the message together with its envelope.

## How to Run

Start the infrastructure as described in <xref:samples>, then run the two applications from the repository root.

```bash
dotnet run --project ./samples/Kafka/Basic.Producer/.
dotnet run --project ./samples/Kafka/Basic.Consumer/.
```

Observe the console output while the messages are produced and consumed.

## Common

The message being exchanged is defined in a common project.

[!code-csharp[Common.Message](../../../samples/Kafka/Basic.Common/SampleMessage.cs)]

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/Basic.Common_


## Producer

The producer uses a hosted service to publish some messages in the background.

# [Startup](#tab/producer-startup)
[!code-csharp[Producer.Startup](../../../samples/Kafka/Basic.Producer/Startup.cs)]
# [BrokerClientsConfigurator](#tab/producer-endpoints)
[!code-csharp[Producer.BrokerClientsConfigurator](../../../samples/Kafka/Basic.Producer/BrokerClientsConfigurator.cs)]
# [Background Service](#tab/producer-background-service)
[!code-csharp[Producer.BackgroundService](../../../samples/Kafka/Basic.Producer/ProducerBackgroundService.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/Basic.Producer_

## Consumer

The consumer processes the messages and outputs their value to the standard output.

# [Startup](#tab/consumer-startup)
[!code-csharp[Consumer.Startup](../../../samples/Kafka/Basic.Consumer/Startup.cs)]
# [BrokerClientsConfigurator](#tab/consumer-endpoints)
[!code-csharp[Consumer.BrokerClientsConfigurator](../../../samples/Kafka/Basic.Consumer/BrokerClientsConfigurator.cs)]
# [Subscriber](#tab/consumer-subscriber)
[!code-csharp[Consumer.Subscriber](../../../samples/Kafka/Basic.Consumer/SampleMessageSubscriber.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/Basic.Consumer_

## Additional Resources

- <xref:broker>
- <xref:producing>
- <xref:consuming>
- <xref:setup>
