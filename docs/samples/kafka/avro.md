---
uid: sample-kafka-avro
---

# Kafka - Avro

This sample implements a producer and consumer which take advantage of the schema registry and serializes the messages as Avro.

## Common

The message being exchanged is defined in a common project.

[!code-csharp[Common.Message](../../../samples/Kafka/Avro.Common/AvroMessage.cs)]

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/Avro.Common_


## Producer

The producer uses a hosted service to publish some messages in the background.

# [Startup](#tab/producer-startup)
[!code-csharp[Producer.Startup](../../../samples/Kafka/Avro.Producer/Startup.cs)]
# [EndpointsConfigurator](#tab/producer-endpoints)
[!code-csharp[Producer.EndpointsConfigurator](../../../samples/Kafka/Avro.Producer/EndpointsConfigurator.cs)]
# [Background Service](#tab/producer-background-service)
[!code-csharp[Producer.BackgroundService](../../../samples/Kafka/Avro.Producer/ProducerBackgroundService.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/Avro.Producer_

## Consumer

The consumer processes the messages and outputs their value to the standard output.

# [Startup](#tab/consumer-startup)
[!code-csharp[Consumer.Startup](../../../samples/Kafka/Avro.Consumer/Startup.cs)]
# [EndpointsConfigurator](#tab/consumer-endpoints)
[!code-csharp[Consumer.EndpointsConfigurator](../../../samples/Kafka/Avro.Consumer/EndpointsConfigurator.cs)]
# [Subscriber](#tab/consumer-subscriber)
[!code-csharp[Consumer.Subscriber](../../../samples/Kafka/Avro.Consumer/AvroMessageSubscriber.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/Avro.Consumer_
