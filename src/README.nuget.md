# Silverback

**Simplicity at its core. Flexibility at its peak. Effortless messaging for .NET.**

Silverback is a message bus and broker integration library to build event-driven architectures with first-class support for **Apache Kafka**
and **MQTT**.

Documentation, guides, and samples are available at **https://silverback-messaging.net**.

## Produce and Consume in a Few Lines

Configure the broker connection, the producer and consumer endpoints, and a subscriber. Silverback takes care of serialization, headers,
error handling and delivery.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer(producer => producer
            .Produce<OrderCreated>(endpoint => endpoint
                .ProduceTo("orders")))
        .AddConsumer(consumer => consumer
            .WithGroupId("billing")
            .Consume<OrderCreated>(endpoint => endpoint
                .ConsumeFrom("orders")
                .OnError(policy => policy.Retry(3).ThenSkip()))))
    .AddSingletonSubscriber<OrderCreatedHandler>();
```

```csharp
// Publish: the message is routed to the "orders" topic
await publisher.PublishAsync(new OrderCreated(orderId));

// Consume: the subscriber is invoked for each consumed message
public class OrderCreatedHandler
{
    public Task HandleAsync(OrderCreated message) => ...;
}
```

Follow the [Quickstart](https://silverback-messaging.net/guides/quickstart.html) for a complete, runnable walkthrough.

## Why Silverback

- **Kafka-first, not Kafka-only** – a consistent, fluent API for Kafka and MQTT that still surfaces partitions, keys, tombstones and
  transactions.
- **Reliable by design** – transactional outbox, error policies (retry, skip, move) and storage-backed offsets and locks.
- **Built for throughput** – partition-based parallelism, batch processing, streaming and chunking of large payloads.
- **Cross-cutting features** – serialization, Schema Registry (JSON, Avro, Protobuf), headers, validation and end-to-end encryption.
- **Observable** – structured logging, distributed tracing via `System.Diagnostics` and health checks.
- **Testable** – in-memory Kafka and MQTT brokers and testing helpers for fast and reliable end-to-end tests.

## Packages

Silverback is modular – reference only what you need.

Core:

- **[Silverback.Core](https://www.nuget.org/packages/Silverback.Core/)** – message bus and core messaging components.
- **[Silverback.Core.Model](https://www.nuget.org/packages/Silverback.Core.Model/)** – message semantics for event-driven/CQRS scenarios.

Broker integration:

- **[Silverback.Integration.Kafka](https://www.nuget.org/packages/Silverback.Integration.Kafka/)** – Kafka support.
- **[Silverback.Integration.MQTT](https://www.nuget.org/packages/Silverback.Integration.MQTT/)** – MQTT support.

Optional features:

- **[Silverback.Core.Rx](https://www.nuget.org/packages/Silverback.Core.Rx/)** – Rx.NET integration.
- **[Silverback.Integration.Newtonsoft](https://www.nuget.org/packages/Silverback.Integration.Newtonsoft/)** – Newtonsoft.Json
  serialization.
- **[Silverback.Integration.Kafka.SchemaRegistry](https://www.nuget.org/packages/Silverback.Integration.Kafka.SchemaRegistry/)** – Confluent
  Schema Registry integration (Avro, Protobuf and JSON).
- **[Silverback.Integration.HealthChecks](https://www.nuget.org/packages/Silverback.Integration.HealthChecks/)** – health checks for the
  consumers and the outbox.

Storage (for outbox, client-side offsets, distributed locks):

- **[Silverback.Storage.PostgreSql](https://www.nuget.org/packages/Silverback.Storage.PostgreSql/)**
- **[Silverback.Storage.Sqlite](https://www.nuget.org/packages/Silverback.Storage.Sqlite/)**
- **[Silverback.Storage.EntityFramework](https://www.nuget.org/packages/Silverback.Storage.EntityFramework/)**
- **[Silverback.Storage.Memory](https://www.nuget.org/packages/Silverback.Storage.Memory/)**

Testing:

- **[Silverback.Integration.Testing](https://www.nuget.org/packages/Silverback.Integration.Testing/)** – broker-independent testing helpers.
- **[Silverback.Integration.Kafka.Testing](https://www.nuget.org/packages/Silverback.Integration.Kafka.Testing/)** – in-memory Kafka broker.
- **[Silverback.Integration.MQTT.Testing](https://www.nuget.org/packages/Silverback.Integration.MQTT.Testing/)** – in-memory MQTT broker.

## Supported .NET Versions

Starting with v5, Silverback targets the **latest .NET LTS** (Long-Term Support) version only. It can run on newer compatible .NET versions
(including STS releases), but older frameworks are no longer targeted or supported.

## Documentation

The quickstart, guides, samples, FAQ and API reference are available at **https://silverback-messaging.net**.

## Community

- [Source code](https://github.com/BEagle1984/silverback) – the GitHub repository.
- [Issues](https://github.com/BEagle1984/silverback/issues) – report bugs or suggest improvements.
- [Discussions](https://github.com/BEagle1984/silverback/discussions) – ask questions and share ideas.
## License

Silverback is open source, licensed under the MIT License and maintained by Sergio Aquilini.
See [LICENSE](https://github.com/BEagle1984/silverback/blob/master/LICENSE).
