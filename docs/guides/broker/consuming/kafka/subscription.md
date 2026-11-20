---
uid: kafka-subscription
description: "Subscribe Kafka consumers to topics by consumer group, by pattern or with static partition assignment, and control partition processing concurrency."
---

# Kafka Subscription and Partition Assignment

Silverback supports the standard Kafka consumer group subscription flow and manual (static) partition assignment.

## Subscribe (Consumer Group)

In consumer group mode, Kafka assigns partitions automatically and balances them across instances with the same group id.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic"))));
```

## Subscribe by Pattern

A topic starting with `^` is a regular expression: the consumer subscribes all the topics matching it, and Kafka balances their partitions across the consumer group, as it does for the topics subscribed by name.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .WithGroupId("my-group")
            .Consume<OrderEvent>("endpoint1", endpoint => endpoint
                .ConsumeFrom("^orders-[a-z]+$"))));
```

Topic names and patterns can be mixed, in the same endpoint or across the endpoints of the same consumer. Each message is routed to the endpoint subscribing its topic by name, if any, and otherwise to the first endpoint (in configuration order) with a matching pattern.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .WithGroupId("my-group")
            .Consume<PaymentEvent>("payments", endpoint => endpoint
                .ConsumeFrom("payments", "^payments-[a-z]+$"))
            .Consume<LegacyOrderEvent>("legacy-orders", endpoint => endpoint
                .ConsumeFrom("orders-legacy"))
            .Consume<OrderEvent>("orders", endpoint => endpoint
                .ConsumeFrom("^orders-[a-z]+$"))));
```

> [!Note]
> In the example above, the messages from `orders-legacy` are routed to the `legacy-orders` endpoint, even though the topic also matches `^orders-[a-z]+$`.

With the classic group protocol, the topics created or deleted after the consumer started are picked up at the next topic metadata refresh, which happens every 5 minutes by default and triggers a rebalance. Use `WithTopicMetadataRefreshIntervalMs` to change the interval. With the consumer group protocol (`WithGroupProtocol(GroupProtocol.Consumer)`), the broker evaluates the pattern and updates the assignment itself.

With the default offset reset (`latest`), the messages written to a new topic before the consumer picks it up are skipped. Use `AutoResetOffsetToEarliest()` to consume them.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .WithGroupId("my-group")
            .WithTopicMetadataRefreshIntervalMs(60000)
            .AutoResetOffsetToEarliest()
            .Consume<OrderEvent>("endpoint1", endpoint => endpoint
                .ConsumeFrom("^orders-[a-z]+$"))));
```

> [!Important]
> The subscribed topics are selected by librdkafka (classic group protocol) or by the broker (consumer group protocol, which requires Apache Kafka 4.0 or later), but Silverback routes the messages by evaluating the same pattern as a .NET regular expression. The consumer fails if it receives a message from a topic that the .NET regular expression doesn't match, so keep the patterns simple and use only syntax that all engines interpret the same way: for example `[0-9]` instead of `\d`, and no lookarounds or lazy quantifiers.
>
> Write the patterns to match the whole topic name, as in `^orders-.*` or `^orders-[a-z]+$`. With the classic group protocol, librdkafka accepts a match at the beginning of the name (`^orders-` matches `orders-eu`), while with the consumer group protocol the broker requires the pattern to match the entire name (`^orders-` only matches a topic named `orders-`). Silverback routes the messages (and the mocked Kafka assigns the topics) following the same rule, according to the configured group protocol.

> [!Note]
> A pattern can only be subscribed: it can't be combined with the static assignment of specific partitions or with a partitions provider (see below).

## Static Assignment

You can manage partition assignment manually by specifying the partitions to consume.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic", 0, 3, 5))));
```

> [!Note]
> The example above assigns partitions 0, 3, and 5 of `my-topic` to this consumer endpoint. Other overloads let you specify starting offsets or consume fixed partitions across multiple topics.

## Static Assignment from Topic Metadata

If you don't want to hardcode partition numbers, you can retrieve topic metadata and select partitions programmatically.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom(
                    "my-topic",
                    partitions => partitions
                        .Where(p => p.Partition % 2 == 0)
                        .Select(p => new TopicPartitionOffset(p, Offset.Beginning))))));
```

## Static Assignment from the Beginning

`ConsumeFromBeginning` statically assigns all the partitions of the specified topics, retrieved from the topic metadata, and consumes them from the beginning, regardless of the offsets committed to Kafka. The overloads accepting specific partitions or a function selecting them are available as well.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .DisableOffsetsCommit()
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFromBeginning("my-topic"))));
```

> [!Note]
> Unlike `ConsumeFrom`, `ConsumeFromBeginning` always assigns the partitions statically, therefore the topic patterns aren't supported. With `DisableOffsetsCommit` no group id is needed. A client side offset store (see <xref:kafka-offset>) still applies, and the partitions are then consumed from the stored offsets. Combine it with the catch-up to be notified once the topics are fully consumed (see <xref:kafka-catch-up>).

## Partition Processing and Concurrency

By default, partitions are processed in parallel (one queue per partition). Messages in the same partition are always processed sequentially.

You can limit concurrency using `LimitParallelism` or disable partition-based parallelism using `ProcessAllPartitionsTogether`.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .ProcessAllPartitionsTogether()
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic"))));
```

## Callbacks

Use broker callbacks to react to partition assignment/revocation events. See <xref:broker-callbacks>.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:consuming>
- <xref:broker-callbacks>
