---
uid: faq
title: FAQ and Troubleshooting
---

# FAQ and Troubleshooting

This page collects the most common questions and issues. If your problem isn't listed here, search the [GitHub issues](https://github.com/BEagle1984/silverback/issues) or ask in the [discussions](https://github.com/BEagle1984/silverback/discussions).

## Consuming

### The Consumer Doesn't Receive the Messages Produced Before It Started

A Kafka consumer group without committed offsets starts from the end of the topic by default (`auto.offset.reset=latest`). Use `AutoResetOffsetToEarliest()` to start from the beginning of the topic instead:

```csharp
.AddConsumer(consumer => consumer
    .WithGroupId("my-group")
    .AutoResetOffsetToEarliest()
    .Consume<MyMessage>(endpoint => endpoint
        .ConsumeFrom("my-topic")))
```

Also double-check the bootstrap servers, the topic name and the group id. Remember that consumers in the same group share the partitions: if another instance with the same group id is running, it might be consuming the messages.

### The Consumer Stops With an `UnhandledMessageException`

By default, both the Kafka and the MQTT consumers throw an <xref:Silverback.Messaging.Publishing.UnhandledMessageException> if no subscriber handles a consumed message. The exception triggers the error policy (and by default stops the consumer). Make sure that:

- the subscriber is registered (e.g. `AddSingletonSubscriber<MySubscriber>()`),
- the subscribed method's parameter type matches the consumed message type (or one of its base types or interfaces),
- the message is deserialized into the expected type (see <xref:deserialization>).

If unhandled messages are expected, call `IgnoreUnhandledMessages()` on the consumer endpoint.

> [!Note]
> This is unrelated to the MQTT producer's behavior when no client is subscribed to the destination topic (see [MQTT Producing When Nobody Is Subscribed](#mqtt-producing-when-nobody-is-subscribed)).

### The Consumer Stopped After an Exception

The default error policy stops the consumer when the processing fails, to avoid losing messages. Configure an error policy to retry, skip or move the failed messages (see <xref:error-policies>), and use the <xref:consumer-health-check> to detect stopped consumers.

### The Kafka Consumer Is Kicked Out of the Group While Processing

If processing a message (including the retries) takes longer than `max.poll.interval.ms`, the consumer is considered dead and its partitions are reassigned. Increase the interval with `WithMaxPollIntervalMs(...)` or reduce the processing time (e.g. smaller batches, shorter retry delays).

### The Messages Aren't Processed in Parallel

Kafka messages from the same partition are always processed sequentially, to preserve their order. The parallelism is therefore bound by the number of assigned partitions (and by `LimitParallelism`). With MQTT, parallel processing must be enabled explicitly via `EnableParallelProcessing`. See <xref:consuming>.

### The Same Message Is Processed More Than Once

Kafka and MQTT (with QoS 1) guarantee at-least-once delivery: a message can be redelivered after a crash, a rebalance or a reconnection, if its offset (or acknowledgment) wasn't committed yet. Make the subscribers idempotent, or use the client-side offset storage together with your database transaction to achieve exactly-once processing (see <xref:kafka-offset>).

## Serialization

### The Message Is Deserialized as the Wrong Type

When consuming with `Consume<TMessage>`, the JSON deserializer uses `TMessage` and ignores the `x-message-type` header by default. To consume different message types from the same topic, enable the message type header in the deserializer (e.g. `DeserializeJson(deserializer => deserializer.WithOptionalMessageTypeHeader())`) and make sure the producer sends it. See <xref:deserialization> and <xref:serialization>.

### Interoperability With Non-Silverback Producers or Consumers

Silverback produces plain JSON by default and adds a few headers (see <xref:default-headers>). The other applications can simply ignore the headers. When consuming from a non-Silverback producer, the `x-message-type` header is missing, so make sure the consumer endpoint specifies the message type via `Consume<TMessage>`.

## Producing

### The Message Is Published But Not Produced

A message is produced only if an endpoint is configured for its type (or for one of its base types or interfaces) via `Produce<TMessage>`. If the message is not routed to any endpoint, it is only published to the in-memory subscribers. Check also any configured filter (see <xref:producing>).

### The Kafka Messages Are Not Ordered

Kafka guarantees ordering only within a partition. Set a key (e.g. via `SetKafkaKey` or the `KafkaKeyMemberAttribute`) to ensure that the related messages land in the same partition. See <xref:kafka-partitioning>.

### MQTT Producing When Nobody Is Subscribed

By default, the MQTT producer doesn't throw if no subscriber is consuming the produced message. The behavior can be configured per endpoint with `IgnoreNoMatchingSubscribers()`, `LogNoMatchingSubscribersWarning()` or `ThrowNoMatchingSubscribersError()`. See <xref:mqtt>.

## Outbox and Storage

### The Outbox Messages Are Stored but Never Produced

The outbox requires the outbox worker to be configured and running (`AddOutboxWorker`), and the endpoint must have a name. Also check that the transaction is committed: when the outbox write is enlisted in a database transaction, the messages become visible to the worker only after the commit. See <xref:outbox>.

## Logging and Diagnostics

### How Can I See What Silverback Is Doing?

Lower the minimum log level of the `Silverback` categories to `Debug`, or change the level of specific events via `WithLogLevels`. All events are listed in <xref:logging>.

## Additional Resources

- <xref:quickstart>
- <xref:consuming>
- <xref:producing>
- <xref:testing>
