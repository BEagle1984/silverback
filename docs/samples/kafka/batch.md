---
uid: sample-kafka-batch
---

# Kafka - Batch Processing

In this sample the consumed messages are processed in batch.

## What This Sample Shows

- Publishing 100 messages at once with `WrapAndPublishBatchAsync`, setting the Kafka key of each envelope.
- Enabling batch processing on the consumer endpoint with `EnableBatchProcessing`, using batches of up to 100 messages and a maximum wait time of 5 seconds.
- Subscribing to an `IAsyncEnumerable<SampleMessage>` to stream through each batch, computing the sum of the numbers it contains.

## How to Run

Start the infrastructure as described in <xref:samples>, then run the two applications from the repository root.

```bash
dotnet run --project ./samples/Kafka/Batch.Producer/.
dotnet run --project ./samples/Kafka/Batch.Consumer/.
```

The consumer logs the size and the sum of each processed batch.

## Common

The message being exchanged is defined in a common project.

[!code-csharp[Common.Message](../../../samples/Kafka/Batch.Common/SampleMessage.cs)]

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/Batch.Common_


## Producer

The producer uses a hosted service to publish some messages in the background.

# [Startup](#tab/producer-startup)
[!code-csharp[Producer.Startup](../../../samples/Kafka/Batch.Producer/Startup.cs)]
# [BrokerClientsConfigurator](#tab/producer-endpoints)
[!code-csharp[Producer.BrokerClientsConfigurator](../../../samples/Kafka/Batch.Producer/BrokerClientsConfigurator.cs)]
# [Background Service](#tab/producer-background-service)
[!code-csharp[Producer.BackgroundService](../../../samples/Kafka/Batch.Producer/ProducerBackgroundService.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/Batch.Producer_

## Consumer

The consumer processes the messages in batch and outputs the batch sum to the standard output.

# [Startup](#tab/consumer-startup)
[!code-csharp[Consumer.Startup](../../../samples/Kafka/Batch.Consumer/Startup.cs)]
# [BrokerClientsConfigurator](#tab/consumer-endpoints)
[!code-csharp[Consumer.BrokerClientsConfigurator](../../../samples/Kafka/Batch.Consumer/BrokerClientsConfigurator.cs)]
# [Subscriber](#tab/consumer-subscriber)
[!code-csharp[Consumer.Subscriber](../../../samples/Kafka/Batch.Consumer/SampleMessageBatchSubscriber.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/Batch.Consumer_

## Additional Resources

- <xref:batch-processing>
- <xref:consuming>
- <xref:producing>
- <xref:sample-kafka-batch-tombstone> sample
