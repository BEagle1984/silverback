---
uid: dekaf
---

# Kafka via Dekaf (Preview)

[Silverback.Integration.Dekaf](https://www.nuget.org/packages/Silverback.Integration.Dekaf) is an alternative to [Silverback.Integration.Kafka](https://www.nuget.org/packages/Silverback.Integration.Kafka) built on [Dekaf](https://github.com/thomhurst/Dekaf), a fully managed .NET Kafka client, instead of Confluent.Kafka.

Since Dekaf doesn't depend on librdkafka, no native binaries are deployed with the application: the deployment is smaller and there's no dependency on the platforms supported by the native builds (e.g. Alpine/musl or uncommon architectures).

> [!WARNING]
> The package is a **preview**. It covers producing, consuming, batching, chunking, error policies and transactions, but some features of the Confluent.Kafka based integration are not available yet (see [Limitations](#limitations)).

## Switching to Dekaf

The package keeps the same namespaces and configuration API, apart from the client settings that aren't supported (see [Differences](#differences-from-the-confluentkafka-based-integration)), so switching usually requires replacing the package reference only:

```powershell
dotnet remove package Silverback.Integration.Kafka
dotnet add package Silverback.Integration.Dekaf --prerelease
```

```csharp
services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(
        clients => clients
            .WithBootstrapServers("PLAINTEXT://localhost:9092")
            .AddProducer(
                producer => producer
                    .Produce<OrderCreated>(endpoint => endpoint.ProduceTo("orders")))
            .AddConsumer(
                consumer => consumer
                    .WithGroupId("order-service")
                    .Consume(endpoint => endpoint.ConsumeFrom("orders"))));
```

> [!IMPORTANT]
> The two packages define the same types and cannot be referenced by the same project.

The code referencing the Confluent.Kafka types needs to be adapted:

* `TopicPartition`, `TopicPartitionOffset`, `Partition`, `Offset`, `Acks` and `AutoOffsetReset` are Silverback types with the same shape, in the `Silverback.Messaging.Broker.Kafka` and `Silverback.Messaging.Configuration.Kafka` namespaces. Replacing `using Confluent.Kafka;` is usually enough.
* The client settings not listed below have no builder method and need to be removed.
* The consumer endpoints can't resolve the partitions to be consumed via a function (`ConsumeFrom` overloads taking a partitions provider): the static partition assignment requires the partitions to be specified explicitly.

## Testing

[Silverback.Integration.Dekaf.Testing](https://www.nuget.org/packages/Silverback.Integration.Dekaf.Testing) replaces Silverback.Integration.Kafka.Testing. It provides the same `AddMockedKafka` / `UseMockedKafka` methods and a similar <xref:Silverback.Testing.IKafkaTestingHelper>, backed by the Dekaf.Testing in-memory cluster. The differences are:

* The produced records are read through `IKafkaTestingHelper.Cluster` (the `InMemoryKafkaCluster`) instead of `GetTopic`.
* `IMockedKafkaOptionsBuilder` only allows to set the default partitions count. The partitions assignment delay, the per-topic partitions count and the auto-commit interval override are not available.
* `IMockedConsumerGroup` can't trigger a rebalance.
* The partition EOF isn't emulated.
* The transactional producers aren't fenced by their transactional id.
* The keys are mapped to the partitions differently than by the real producer, which uses the `consistent_random` partitioner.

## Requirements

* **Kafka 4.0 or later.** Dekaf only implements the KIP-848 consumer group protocol, so it can't join a consumer group on an older broker.
* **Cooperative rebalancing only.** With KIP-848 the partition assignment is computed by the broker and a rebalance only revokes the partitions that move to another member. The partition assignment strategy can't be configured.

## Differences from the Confluent.Kafka Based Integration

* The configuration exposes a subset of the client settings: the bootstrap servers, the client id, the consumer group id and instance id, the auto offset reset, the auto-commit interval, the partition EOF events, the acks, the idempotence, the linger and the transactional id.
* The consumers read only the committed messages of transactional producers (`read_committed`), as with librdkafka.
* The producer uses the librdkafka default partitioner (`consistent_random`), so that the messages with the same key are written to the same partition as with Confluent.Kafka.
* Without an explicit idempotence setting, the idempotence (enabled by default by Dekaf) is disabled when `Acks` is set to `None` or `Leader`, since it requires all acks.
* The partitions assigned/revoked callbacks are supported (see [Limitations](#limitations) for the other callbacks).

## Limitations

* The storage packages (`Silverback.Storage.*`, needed e.g. for the outbox and the client-side offset storage) reference Silverback.Integration.Kafka for the Kafka offset stores and can therefore not be used together with this package yet.
* No Schema Registry integration yet (the Confluent serializers depend on Confluent.Kafka).
* No statistics, log, error and offsets committed callbacks, since Dekaf doesn't expose the corresponding events: use the Dekaf OpenTelemetry metrics and the `ILogger` output instead.
* The static partition assignment requires the partitions to be specified explicitly: resolving them via a partitions provider is not supported yet.
* Only the client settings listed above can be configured: SASL, TLS and the other security settings are not available yet.

### Known Issues

Dekaf invokes the partitions assigned/revoked callbacks from its heartbeat loop, concurrently with the polling, while librdkafka invokes them from within the poll. The consumer is ported from the Confluent.Kafka based integration and isn't fully adapted to this model yet:

* With `ProcessPartitionsIndependently` disabled, a record polled while another partition is being revoked may be skipped or processed out of order.
* The revoke callback waits for the in-flight messages of the revoked partitions to be processed and blocks the heartbeats meanwhile: a handler running longer than the session timeout (45 seconds by default) causes the member to be fenced and to lose all its partitions.
* The partitions assigned while the consumer is stopped start from the committed offsets: the offsets returned by the partitions assigned callbacks and the client-side offset store are ignored.
