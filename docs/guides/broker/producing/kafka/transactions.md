---
uid: kafka-transactions
---

# Kafka Transactions

Silverback supports Kafka transactions, allowing you to produce messages atomically across multiple topics or partitions. This is particularly useful when you need to ensure that a set of messages is either fully processed or not processed at all, maintaining data integrity.

> [!Important]
> A Kafka transaction cannot span multiple producers. Therefore, you must configure all messages that needs to be produced in the same transaction to be sent by the same producer.

## Enabling Transactions

To enable transactions in Silverback, you need to configure the producer to use transactions. 

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

To use transactions, you can wrap your message production in a transaction scope. This ensures that all messages produced within the scope are part of the same transaction.

```csharp
using (IKafkaTransaction transaction = publisher.InitKafkaTransaction())
{
    await publisher.PublishEventAsync(new MyMessage());
    await publisher.PublishEventAsync(new MyMessage());
    await publisher.PublishEventAsync(new AnotherMessage());

    transaction.Commit();
}
```

## Committing Consumed Offsets in the Transaction

In a typical consume-transform-produce scenario, you might want to commit the consumed offsets as part of the transaction. Silverback allows you to do this simply configuring the consumer with `SendOffsetsToTransaction`.

> [!Important]
> Once `SendOffsetsToTransaction` is enabled, the offsets will be committed only when the transaction is committed. If no transaction is initialized, the offsets will not be committed.

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

* [API Reference](xref:Silverback)
