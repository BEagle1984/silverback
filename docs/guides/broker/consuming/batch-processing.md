---
uid: batch-processing
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

> [!Note]
> Batch processing is currently supported for Kafka only.

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
> A batch per topic partition is created by default. You can decide to process all partitions together by calling `ProcessAllPartitionsTogether` on the consumer configuration and that will create a single batch for the whole topic.

> [!Tip]
> While the batch is enumerated, the current `Activity` continues the trace of the message being processed. Avoid buffering the batch (e.g. with `ToList()`) if the trace of each message must be preserved. See <xref:consuming-tracing> for details.

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

- With Kafka, a stream per partition is created by default, or a single one for all the partitions with `ProcessAllPartitionsTogether`.
- Each message is committed as soon as the subscriber requests the next one.
- When the consumer is stopped or disconnected, or the partitions are revoked, the enumeration throws an `OperationCanceledException` (an `IMessageStreamObservable<T>` completes, and `Subscribe` then throws it). A new stream is published, invoking the subscriber again, when the consumption resumes.
- If the subscriber throws while processing a message, that message isn't committed and the consumer is stopped and disconnected, as with the default stop policy (see <xref:error-policies>). The other error policies can't be used with streaming.

> [!Warning]
> Process the messages one at a time. Since each message is committed as soon as the next one is requested, consuming the stream concurrently (for example with `Parallel.ForEachAsync`) commits the messages before they're processed, and an exception thrown while the stream is waiting for the next message may not stop the consumer.

> [!Warning]
> Don't stop enumerating the stream (for example with a `break` or an early `return`) while the consumer is running. The message being processed is never committed and the partition stalls, while stopping or disconnecting the consumer, including the graceful shutdown of the application, hangs.

> [!Warning]
> The subscriber is resolved from the service scope of the message that starts the stream, and that scope stays open until the stream ends, which can be the whole lifetime of the application. The scoped services, such as an Entity Framework `DbContext`, are therefore shared by all the messages of the stream: their state keeps growing and is never refreshed. Inject the `IServiceScopeFactory` instead, and create a scope per message or per unit of work.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:consuming-tracing> guide
- <xref:sample-kafka-batch> sample
