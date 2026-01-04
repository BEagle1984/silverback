---
uid: kafka-transactions
---

# Kafka Transactions

Kafka transactions let you produce messages atomically: either all messages in the transaction are published, or none are.

This is useful for multi-topic writes and consume-transform-produce scenarios.

> [!Important]
> A Kafka transaction cannot span multiple producers. Configure all messages that must be part of the same transaction to be sent by the same producer.

## Enabling Transactions

Enable transactions on the producer by setting a `transactional.id` (via `EnableTransactions`).

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .EnableTransactions("transactional-id")
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic"))
            .Produce<AnotherMessage>("endpoint2", endpoint => endpoint
                .ProduceTo("my-other-topic")))));
```

## Using Transactions

Initialize a transaction, publish messages, then commit.

```csharp
using IKafkaTransaction transaction = publisher.InitKafkaTransaction();

await publisher.PublishEventAsync(new MyMessage());
await publisher.PublishEventAsync(new MyMessage());
await publisher.PublishEventAsync(new AnotherMessage());

transaction.Commit();
```

> [!Note]
> If you don’t commit the transaction, it will be aborted when the transaction is disposed.

## Committing Consumed Offsets in a Transaction

In consume-transform-produce scenarios you may want Kafka offsets to be committed only when the transaction is committed.

Enable it on the consumer with `SendOffsetsToTransaction`.

> [!Important]
> When `SendOffsetsToTransaction` is enabled, offsets are committed as part of the active Kafka transaction. If no transaction is active, offsets are not committed.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .EnableTransactions("transactional-id")
            .Produce<OutputMessage>("endpoint-out", endpoint => endpoint
                .ProduceTo("out-topic")))
        .AddConsumer("consumer1", consumer => consumer
            .Consume<InputMessage>("endpoint-in", endpoint => endpoint
                .ConsumeFrom("in-topic"))
            .SendOffsetsToTransaction())));
```

## Additional Resources

- [API Reference](xref:Silverback)
