---
uid: sample-kafka-transactional-producer
---

# Kafka - Transactional Producer

This sample demonstrates how to produce messages atomically using Kafka transactions, committing some transactions and aborting others.

## What This Sample Shows

- Enabling transactions on the producer with `EnableTransactions`, specifying the transactional id.
- Starting a transaction with `InitKafkaTransaction`, publishing a set of 5 messages and committing it with `Commit`.
- Aborting a transaction by disposing it without committing: an exception is thrown every few iterations (whenever the first number is a multiple of 3000), so the transaction is never committed.
- Running two hosted services that use transactions concurrently within the same application.
- Consuming the topic with a plain consumer, which retries each message twice and then skips it in case of error.

## How to Run

Start the infrastructure as described in <xref:samples>, then run the two applications from the repository root.

```bash
dotnet run --project ./samples/Kafka/TransactionalProducer.Producer/.
dotnet run --project ./samples/Kafka/TransactionalProducer.Consumer/.
```

The producer logs when each transaction is initialized, when its messages are produced and when it is committed. Each transaction stays open for 3 seconds before being committed or aborted, and the consumer receives the messages only once their transaction is committed.

## Common

The message being exchanged is defined in a common project.

[!code-csharp[Common.Message](../../../samples/Kafka/TransactionalProducer.Common/SampleMessage.cs)]

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/TransactionalProducer.Common_

## Producer

The producer uses two hosted services to publish the messages in the background, each one wrapping every set of messages in a Kafka transaction.

# [Startup](#tab/producer-startup)
[!code-csharp[Producer.Startup](../../../samples/Kafka/TransactionalProducer.Producer/Startup.cs)]
# [BrokerClientsConfigurator](#tab/producer-endpoints)
[!code-csharp[Producer.BrokerClientsConfigurator](../../../samples/Kafka/TransactionalProducer.Producer/BrokerClientsConfigurator.cs)]
# [Background Service](#tab/producer-background-service)
[!code-csharp[Producer.BackgroundService](../../../samples/Kafka/TransactionalProducer.Producer/ProducerBackgroundService.cs)]
# [Background Service 2](#tab/producer-background-service-2)
[!code-csharp[Producer.BackgroundService2](../../../samples/Kafka/TransactionalProducer.Producer/ProducerBackgroundService2.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/TransactionalProducer.Producer_

## Consumer

The consumer processes the messages and outputs their value to the standard output.

# [Startup](#tab/consumer-startup)
[!code-csharp[Consumer.Startup](../../../samples/Kafka/TransactionalProducer.Consumer/Startup.cs)]
# [BrokerClientsConfigurator](#tab/consumer-endpoints)
[!code-csharp[Consumer.BrokerClientsConfigurator](../../../samples/Kafka/TransactionalProducer.Consumer/BrokerClientsConfigurator.cs)]
# [Subscriber](#tab/consumer-subscriber)
[!code-csharp[Consumer.Subscriber](../../../samples/Kafka/TransactionalProducer.Consumer/SampleMessageSubscriber.cs)]
***

_Full source code: https://github.com/BEagle1984/silverback/tree/master/samples/Kafka/TransactionalProducer.Consumer_

## Additional Resources

- <xref:kafka-transactions>
- <xref:producing>
- <xref:consuming>
