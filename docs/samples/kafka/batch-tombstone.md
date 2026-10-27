---
uid: sample-kafka-batch-tombstone
---

# Kafka - Batch Processing with Tombstones

In this sample the producer publishes batches of messages that include some tombstones (messages with a key but a `null` value) and the consumer processes them in batch, telling the tombstones apart from the regular messages.

## What This Sample Shows

- Publishing a batch with `WrapAndPublishBatchAsync`, mapping each source item to a message and returning `null` (every number divisible by 30) to produce a tombstone.
- Setting the Kafka key of each envelope (`N<number>`) so that the tombstones still carry a key.
- Consuming in batches of up to 100 messages (or whatever is received within 5 seconds) with `EnableBatchProcessing`.
- Subscribing to a stream of `IInboundEnvelope<SampleMessage>` and checking `IsTombstone` to handle the tombstones, using `GetKafkaKey` to read the key.

## How to Run

Start the infrastructure as described in <xref:samples>, then run the two applications from the repository root.

```bash
dotnet run --project ./samples/Kafka/BatchWithTombstone.Producer/.
dotnet run --project ./samples/Kafka/BatchWithTombstone.Consumer/.
```

The consumer logs every received message or tombstone together with its key and then the size of each processed batch.

## Common

The message being exchanged is defined in a common project.

[!code-csharp[Common.Message](../../../samples/Kafka/BatchWithTombstone.Common/SampleMessage.cs)]

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/BatchWithTombstone.Common_

## Producer

The producer uses a hosted service to publish batches of 100 messages in the background, replacing some of them with tombstones.

# [Startup](#tab/producer-startup)
[!code-csharp[Producer.Startup](../../../samples/Kafka/BatchWithTombstone.Producer/Startup.cs)]
# [BrokerClientsConfigurator](#tab/producer-endpoints)
[!code-csharp[Producer.BrokerClientsConfigurator](../../../samples/Kafka/BatchWithTombstone.Producer/BrokerClientsConfigurator.cs)]
# [Background Service](#tab/producer-background-service)
[!code-csharp[Producer.BackgroundService](../../../samples/Kafka/BatchWithTombstone.Producer/ProducerBackgroundService.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/BatchWithTombstone.Producer_

## Consumer

The consumer processes the messages in batch, logging the key of each tombstone and the key and number of each regular message.

# [Startup](#tab/consumer-startup)
[!code-csharp[Consumer.Startup](../../../samples/Kafka/BatchWithTombstone.Consumer/Startup.cs)]
# [BrokerClientsConfigurator](#tab/consumer-endpoints)
[!code-csharp[Consumer.BrokerClientsConfigurator](../../../samples/Kafka/BatchWithTombstone.Consumer/BrokerClientsConfigurator.cs)]
# [Subscriber](#tab/consumer-subscriber)
[!code-csharp[Consumer.Subscriber](../../../samples/Kafka/BatchWithTombstone.Consumer/SampleMessageBatchSubscriber.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/BatchWithTombstone.Consumer_

## Additional Resources

- <xref:producing-tombstone>
- <xref:consuming-tombstone>
- <xref:batch-processing>
- <xref:sample-kafka-batch> sample
