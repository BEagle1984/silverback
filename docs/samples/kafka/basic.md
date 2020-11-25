# Kafka - Basic

This sample implements the simple possible producer and consumer.

See also: <xref:message-broker>

## Common

The message being exchanged is defined in a common project.

[!code-csharp[Common.Message](../../../samples/Kafka/Basic.Common/SampleMessage.cs)]

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/Basic.Common_


## Producer

The producer uses an hosted service to publish some messages in the background.

# [Startup](#tab/producer-startup)
[!code-csharp[Producer.Startup](../../../samples/Kafka/Basic.Producer/Startup.cs)]
# [Endpoints Configuration](#tab/producer-endpoints)
[!code-csharp[Producer.EndpointsConfigurator](../../../samples/Kafka/Basic.Producer/EndpointsConfigurator.cs)]
# [Background Service](#tab/producer-background-service)
[!code-csharp[Producer.BackgroundService](../../../samples/Kafka/Basic.Producer/ProducerBackgroundService.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/Basic.Producer_

## Consumer

The consumer simply streams the file to a temporary folder in the local file system.

# [Startup](#tab/consumer-startup)
[!code-csharp[Consumer.Startup](../../../samples/Kafka/Basic.Consumer/Startup.cs)]
# [Endpoints Configuration](#tab/consumer-endpoints)
[!code-csharp[Consumer.EndpointsConfigurator](../../../samples/Kafka/Basic.Consumer/EndpointsConfigurator.cs)]
# [Subscriber](#tab/consumer-subscriber)
[!code-csharp[Consumer.Subscriber](../../../samples/Kafka/Basic.Producer/SampleMessageSubscriber.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/Basic.Consumer_