---
uid: example-kafka-binaryfile
---

# Kafka - Files Streaming

This sample demonstrates how to deal with raw binary contents and large messages, to transfer some files through Kafka.

See also: <xref:binary-files>, <xref:chunking>

## Producer

The producer exposes two REST API that receive the path of a local file to be streamed. The second API uses a custom `BinaryMessage` to forward further metadata (the file name in this example).

# [Startup](#tab/producer-startup)
[!code-csharp[Producer.Startup](../../../samples/Kafka/BinaryFileStreaming.Producer/Startup.cs)]
# [BrokerClientsConfigurator](#tab/producer-endpoints)
[!code-csharp[Producer.BrokerClientsConfigurator](../../../samples/Kafka/BinaryFileStreaming.Producer/BrokerClientsConfigurator.cs)]
# [CustomBinaryFileMessage](#tab/producer-custom-message)
[!code-csharp[Producer.CustomBinaryFileMessage](../../../samples/Kafka/BinaryFileStreaming.Producer/Messages/CustomBinaryMessage.cs)]
# [API Controller](#tab/producer-controller)
[!code-csharp[Producer.ProducerController](../../../samples/Kafka/BinaryFileStreaming.Producer/Controllers/ProducerController.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/BinaryFileStreaming.Producer_

## Consumer

The consumer simply streams the file to a temporary folder in the local file system.

# [Startup](#tab/consumer-startup)
[!code-csharp[Consumer.Startup](../../../samples/Kafka/BinaryFileStreaming.Consumer/Startup.cs)]
# [BrokerClientsConfigurator](#tab/consumer-endpoints)
[!code-csharp[Consumer.BrokerClientsConfigurator](../../../samples/Kafka/BinaryFileStreaming.Consumer/BrokerClientsConfigurator.cs)]
# [CustomBinaryFileMessage](#tab/consumer-custom-message)
[!code-csharp[Consumer.CustomBinaryFileMessage](../../../samples/Kafka/BinaryFileStreaming.Consumer/Messages/CustomBinaryMessage.cs)]
# [Subscriber](#tab/consumer-subscriber)
[!code-csharp[Consumer.BinaryFileSubscriber](../../../samples/Kafka/BinaryFileStreaming.Consumer/Subscribers/BinaryFileSubscriber.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/BinaryFileStreaming.Consumer_
