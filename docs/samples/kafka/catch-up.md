---
uid: sample-kafka-catch-up
description: "A Silverback Kafka sample that loads an in-memory product catalog from a topic at startup: the consumer reads all partitions from the beginning and notifies once it's caught up, then keeps the catalog up to date."
---

# Kafka - Catch-Up

This sample loads the in-memory state of a service from a Kafka topic: the consumer reads the whole topic at every start and notifies once all the messages written so far are processed (see <xref:kafka-catch-up>).

## What This Sample Shows

- Producing a bunch of price changes of a set of products, using the product id as message key.
- Assigning all the partitions of the topic statically with `ConsumeFromBeginning`, without a consumer group and with the offsets commit disabled, so that the whole topic is consumed at every start.
- Enabling the catch-up with `NotifyWhenCaughtUp`, to keep consuming (and updating the catalog) once caught up.
- Getting notified via an `IKafkaConsumerCaughtUpCallback`.
- Awaiting `WaitUntilCaughtUpAsync` in a `BackgroundService` that must not use the catalog before it's loaded.

## How to Run

Start the infrastructure as described in <xref:samples>, then run the producer from the repository root: it produces 1000 price changes and exits.

```bash
dotnet run --project ./samples/Kafka/CatchUp.Producer/.
```

Then run the consumer.

```bash
dotnet run --project ./samples/Kafka/CatchUp.Consumer/.
```

Observe the console output: the callback logs that the catalog is loaded, while the background service, which waited for the catch-up, periodically reports the content of the catalog. Run the producer again, in another terminal, to see the catalog kept up to date, or restart the consumer to see it loaded again from the beginning of the topic.

> [!Note]
> The producer stops after a bunch of messages, because a partition that keeps receiving messages more often than `fetch.wait.max.ms` may never report its end (see <xref:kafka-catch-up>).

## Common

The message being exchanged is defined in a common project.

[!code-csharp[Common.Message](../../../samples/Kafka/CatchUp.Common/ProductPriceChanged.cs)]

_Full source code: https://github.com/BEagle1984/silverback/tree/main/samples/Kafka/CatchUp.Common_

## Producer

The producer uses a hosted service to change the price of a random product 1000 times, then stops the application.

# [Startup](#tab/producer-startup)
[!code-csharp[Producer.Startup](../../../samples/Kafka/CatchUp.Producer/Startup.cs)]
# [BrokerClientsConfigurator](#tab/producer-endpoints)
[!code-csharp[Producer.BrokerClientsConfigurator](../../../samples/Kafka/CatchUp.Producer/BrokerClientsConfigurator.cs)]
# [Background Service](#tab/producer-background-service)
[!code-csharp[Producer.BackgroundService](../../../samples/Kafka/CatchUp.Producer/ProducerBackgroundService.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/main/samples/Kafka/CatchUp.Producer_

## Consumer

The consumer applies each price change to the in-memory catalog. The callback logs once the catalog is loaded, while the background service waits for the catch-up before reporting the content of the catalog.

# [Startup](#tab/consumer-startup)
[!code-csharp[Consumer.Startup](../../../samples/Kafka/CatchUp.Consumer/Startup.cs)]
# [BrokerClientsConfigurator](#tab/consumer-endpoints)
[!code-csharp[Consumer.BrokerClientsConfigurator](../../../samples/Kafka/CatchUp.Consumer/BrokerClientsConfigurator.cs)]
# [Catalog](#tab/consumer-catalog)
[!code-csharp[Consumer.Catalog](../../../samples/Kafka/CatchUp.Consumer/ProductCatalog.cs)]
# [Subscriber](#tab/consumer-subscriber)
[!code-csharp[Consumer.Subscriber](../../../samples/Kafka/CatchUp.Consumer/ProductPriceChangedSubscriber.cs)]
# [Callback](#tab/consumer-callback)
[!code-csharp[Consumer.Callback](../../../samples/Kafka/CatchUp.Consumer/CatalogLoadedCallback.cs)]
# [Background Service](#tab/consumer-background-service)
[!code-csharp[Consumer.BackgroundService](../../../samples/Kafka/CatchUp.Consumer/CatalogReportService.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/main/samples/Kafka/CatchUp.Consumer_

## Additional Resources

- <xref:kafka-catch-up>
- <xref:kafka-subscription>
- <xref:broker-callbacks>
