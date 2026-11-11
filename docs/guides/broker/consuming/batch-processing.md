---
uid: batch-processing
description: "Process consumed Kafka messages in batches to cut per-message overhead: configure batch size and timeout, subscribe to batches, or stream them instead."
---

# Batch Processing Consumed Messages

Process multiple consumed messages in a single batch to reduce per-message overhead (DB commits, network IO, etc.). Use batching when throughput matters and a small additional latency is acceptable.

<figure>
	<a href="~/images/diagrams/inbound-batch.png"><img src="~/images/diagrams/inbound-batch.png" alt="Batch processing diagram"/></a>
    <figcaption>Messages are collected and delivered as batches.</figcaption>
</figure>

## Consumer Configuration

Enable batch processing by calling `EnableBatchProcessing` on the consumer endpoint.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .EnableBatchProcessing(1000, TimeSpan.FromSeconds(30)))));
```

With MQTT, batch processing requires MQTT 5 (see [MQTT](#mqtt) below).

## Subscribing to Batches

Your subscriber can consume a batch of messages as an `IAsyncEnumerable<T>` (or `IEnumerable<T>` / `IMessageStreamEnumerable<T>`) of either the message type or the <xref:Silverback.Messaging.Messages.IInboundEnvelope`1>.

```csharp
public async Task HandleAsync(IAsyncEnumerable<MyMessage> messages)
{
    await foreach (MyMessage msg in messages)
    {
        // process each message
    }

    // commit once for the whole batch
    await _db.SaveChangesAsync();
}
```
```csharp
public async Task HandleAsync(IAsyncEnumerable<IInboundEnvelope<MyMessage>> envelopes)
{
    await foreach (IInboundEnvelope<MyMessage> envelope in envelopes)
    {
        if (envelope.IsTombstone)
        {
            // delete the related entity
        }
        else
        {
            // create or update the entity
        }
    }

    // commit once for the whole batch
    await _db.SaveChangesAsync();
}
```

> [!Important]
> All messages in the batch are committed once the subscriber successfully completes.
> If the subscriber throws, the configured error policy applies to the whole batch.

> [!Note]
> With Kafka, a batch per topic partition is created by default. You can decide to process all partitions together by calling `ProcessAllPartitionsTogether` on the consumer configuration and that will create a single batch for all the topics consumed by the consumer, which must then use the same batch settings for all its endpoints (see [Parallelism](xref:consuming#parallelism)).

> [!Tip]
> While the batch is enumerated, the current `Activity` continues the trace of the message being processed. Avoid buffering the batch (e.g. with `ToList()`) if the trace of each message must be preserved. See <xref:consuming-tracing> for details.

## MQTT

Batch processing requires MQTT 5 and is configured in the same way:

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp("localhost")
        .AddClient("my-client", client => client
            .WithClientId("client1")
            .EnableParallelProcessing(4)
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("messages/my")
                .WithAtLeastOnceQoS()
                .EnableBatchProcessing(100, TimeSpan.FromSeconds(5)))));
```

- Each parallel execution (see `EnableParallelProcessing` in [Parallelism](xref:consuming#parallelism)) builds its own batch, and the subscriber processes up to `MaxDegreeOfParallelism` batches at the same time. The messages are spread round-robin across them, so each batch fills that many times more slowly: at low message rates, the batches are usually completed by the max wait time rather than by the batch size. Use a single parallel execution (the default) to preserve the order of the messages.
- All the consumer endpoints of the client share the same batches and must therefore use the same batch settings. A batch can contain messages from different endpoints: handle it with a single subscriber of a common message type and use the same error policy for all the endpoints, since the policy of the endpoint of the first message of the batch applies to the whole batch. Use separate clients for the endpoints that need different settings.
- The messages are acknowledged when their batch is processed. With QoS 1 or 2, the broker stops delivering when too many messages are waiting for acknowledgment, which could prevent a batch from being completed: the max wait time is therefore required, and the batch size multiplied by the `MaxDegreeOfParallelism` must not exceed the `ReceiveMaximum` of the client (65,535 by default, see `LimitUnacknowledgedPublications`). The broker can also enforce a lower limit (e.g. `max_inflight_messages` in Mosquitto): check its configuration. The consumer logs the minimum number of unacknowledged messages the broker must allow when it's created.
- Chunked messages can't be consumed with batch processing: each chunk is rejected and handled by the error policy (see <xref:consuming-chunking>).
- MQTT can't seek: when a batch is rolled back to be retried (e.g. by the `Retry` error policy), its messages are replayed from memory, in the same order. The messages of a batch that is pending when the client disconnects are replayed by the broker, provided that a persistent session and QoS 1 or 2 are used.

## Multiple Subscribers

When multiple subscribers handle the same batch (see also <xref:consuming>), each of them receives its own stream, with the messages of the batch that match its message type and filters:

- The messages are pushed to all the streams at once, so the slowest subscriber sets the pace.
- The batch is completed and committed only when all the subscribers have returned.
- If one subscriber throws while the batch is still being received, the enumeration of the others is canceled. If it throws afterwards (for example when saving the changes after the loop), the others aren't interrupted and run to completion.
- In both cases the error policy is applied to the whole batch (see [Batches, Chunking and Sequences](xref:error-policies#batches-chunking-and-sequences)). A retry invokes all the subscribers again, with a new stream starting from the first message of the batch.

> [!Warning]
> Only one subscriber per batch is supported reliably. If a subscriber stops enumerating before the end of the batch (for example with a `break` or an early `return`) while another subscriber is still enumerating, the batch can neither be completed nor aborted.
>
> The partition then stalls and, once its buffer is full (see `LimitBackpressure` in <xref:consuming>), the consumer stops polling and no partition receives new messages. With `ProcessAllPartitionsTogether` there is a single batch for all the partitions, so the whole consumer stalls right away. The stalled partition also never releases its `LimitParallelism` slot: with `LimitParallelism(1)` and multiple partitions, none of the others is processed anymore. The max wait time doesn't reliably release the batch and stopping the consumer, including the graceful shutdown of the application, waits for it as well, so the process has to be terminated and restarted.
>
> If several actions are needed, use a single subscriber and invoke them explicitly from it.

## Streaming

As an alternative to batch processing, `AllowStreaming` lets a subscriber consume all the messages of an endpoint through a single, unbounded stream. Prefer batch processing whenever possible: streaming is more fragile and some features, like the error policies, aren't available.

```csharp
.AddConsumer("consumer1", consumer => consumer
    .Consume<MyMessage>("endpoint1", endpoint => endpoint
        .ConsumeFrom("my-topic")
        .AllowStreaming()))
```
```csharp
public async Task HandleAsync(IAsyncEnumerable<MyMessage> messages)
{
    await foreach (MyMessage message in messages)
    {
        // process each message
    }
}
```

The subscriber is invoked when the first message is consumed and receives all the following messages through the same stream. It can subscribe to the same types as for the batches, or to an `IMessageStreamObservable<T>` after calling `AsObservable()` from the [Silverback.Core.Rx](https://www.nuget.org/packages/Silverback.Core.Rx) package.

- With Kafka, a stream per partition is created by default, or a single one for all the partitions with `ProcessAllPartitionsTogether`, in which case the streaming endpoint must be the only endpoint of the consumer.
- Each message is committed as soon as the subscriber requests the next one.
- When the consumer is stopped or disconnected, or the partitions are revoked, the enumeration throws an `OperationCanceledException` (an `IMessageStreamObservable<T>` completes, and `Subscribe` then throws it). A new stream is published, invoking the subscriber again, when the consumption resumes.
- If the subscriber throws while processing a message, that message isn't committed and the consumer is stopped and disconnected, as with the default stop policy (see <xref:error-policies>). If it throws while the stream is waiting for the next message, the consumer is stopped and disconnected as well, and the stop callbacks receive the message that started the stream (or the message being added to it at that moment). The other error policies can't be used with streaming.
- If the subscriber stops enumerating the stream (for example with a `break` or an early `return`), the stream ends and the last message it received is committed, unlike when it throws. The next message, including one it stopped waiting for, starts a new stream, invoking the subscriber again. If the subscriber returns without receiving any message, the message that started the stream is committed as well. If it returns because the consumer is stopping (for example when the cancellation token passed to it is canceled), the message is rolled back instead, as if it threw the cancellation, without applying the stop policy.

> [!Warning]
> The stream ends as soon as the subscriber returns. Don't hand it over to a background task: the messages would be committed without being processed.

> [!Warning]
> Process the messages one at a time. Since each message is committed as soon as the next one is requested, consuming the stream concurrently (for example with `Parallel.ForEachAsync`) commits the messages before they're processed, and those whose processing fails aren't consumed again.

> [!Warning]
> Use a single stream subscriber per streaming endpoint: only one subscriber per stream is supported reliably. All the stream subscribers matching the consumed messages share the same stream, including the subscribers of a different message type once they received a message. If a subscriber stops enumerating the stream right when another subscriber receives its first message, the stream can end while that message is still being processed: it might be committed anyway, and a failure processing it isn't handled. If a subscriber stops enumerating the stream while another subscriber is still enumerating it, the stream can neither be completed nor aborted. The partition then stalls, while stopping or disconnecting the consumer, including the graceful shutdown of the application, hangs unless the other subscriber stops when the cancellation token passed to it is canceled.

> [!Warning]
> The subscriber is resolved from the service scope of the message that starts the stream, and that scope stays open until the stream ends, which can be the whole lifetime of the application. The scoped services, such as an Entity Framework `DbContext`, are therefore shared by all the messages of the stream: their state keeps growing and is never refreshed. Inject the `IServiceScopeFactory` instead, and create a scope per message or per unit of work.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:consuming-tracing> guide
- <xref:sample-kafka-batch> sample
