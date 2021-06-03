---
uid: sample-kafka-batch
---

# Kafka - Batch Processing

In this sample the consumed messages are processed in batch.

See also: [Inbound Endpoint - Batch processing](xref:inbound#batch-processing)

## Common

The message being exchanged is defined in a common project.

[!code-csharp[Common.Message](../../../samples/Kafka/Batch.Common/SampleMessage.cs)]

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/Batch.Common_


## Producer

The producer uses a hosted service to publish some messages in the background.

# [Startup](#tab/producer-startup)
[!code-csharp[Producer.Startup](../../../samples/Kafka/Batch.Producer/Startup.cs)]
# [EndpointsConfigurator](#tab/producer-endpoints)
[!code-csharp[Producer.EndpointsConfigurator](../../../samples/Kafka/Batch.Producer/EndpointsConfigurator.cs)]
# [Background Service](#tab/producer-background-service)
[!code-csharp[Producer.BackgroundService](../../../samples/Kafka/Batch.Producer/ProducerBackgroundService.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/Batch.Producer_

## Consumer

The consumer processes the messages in batch and outputs the batch sum to the standard output.

# [Startup](#tab/consumer-startup)
[!code-csharp[Consumer.Startup](../../../samples/Kafka/Batch.Consumer/Startup.cs)]
# [EndpointsConfigurator](#tab/consumer-endpoints)
[!code-csharp[Consumer.EndpointsConfigurator](../../../samples/Kafka/Batch.Consumer/EndpointsConfigurator.cs)]
# [Subscriber](#tab/consumer-subscriber)
[!code-csharp[Consumer.Subscriber](../../../samples/Kafka/Batch.Consumer/SampleMessageBatchSubscriber.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/Batch.Consumer_
