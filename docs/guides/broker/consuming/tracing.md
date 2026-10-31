---
uid: consuming-tracing
---

# Distributed Tracing and Consumers

In .NET, distributed tracing is built around the concept of [Activity](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity), which represents a unit of work in the system. When your application processes an incoming request (e.g. an HTTP call) or handles an incoming message from a broker, an `Activity` can be started to capture timing information and correlation context. This enables end-to-end traceability across services and components.

When trace context is propagated, the .NET runtime and ASP.NET Core follow the [W3C Trace Context specification](https://www.w3.org/TR/trace-context/) and use standard headers such as `traceparent` and `tracestate`, plus optional baggage to enrich the trace.

Similarly, Silverback makes consuming traced messages a seamless experience. If the incoming message contains a trace context, Silverback automatically extracts it and continues the distributed trace when invoking your message handler. This means that:
* The current `Activity` is created/continued using the incoming `traceparent` and `tracestate` values.
* Any propagated baggage is restored so it’s available while processing the message (depending on the transport).
* Any additional work performed while handling the message (database calls, outbound HTTP calls, producing new messages, etc.) naturally becomes part of the same trace.

In other words, if your producers propagate tracing context (which Silverback also does automatically), consumers don’t require any special code: the trace simply flows through your message handling pipeline.

> [!Important]
> This works out of the box with Kafka and MQTT 5 (leveraging user properties), while previous MQTT versions don't support custom headers and therefore can't propagate the trace context.

## Batch Processing

When consuming a batch (see <xref:batch-processing>), each message carries its own trace context. While the subscriber enumerates the batch, Silverback updates the current `Activity` to continue the trace of the message being enumerated, so that any work performed while handling that message becomes part of its trace.

This also applies when the batch is streamed directly into `WrapAndPublishBatchAsync` (or `WrapAndPublishBatch`): each produced message continues the trace of the consumed message it was mapped from.

```csharp
public Task HandleAsync(IAsyncEnumerable<MyMessage> messages) =>
    _publisher.WrapAndPublishBatchAsync(
        messages,
        message => new MyOtherMessage { Id = message.Id });
```

> [!Warning]
> The trace context follows the enumeration. If the batch is buffered before being processed (e.g. using `ToList()`, `ToArray()` or `Chunk()`), the current `Activity` continues the trace of the last enumerated message and all subsequent work, including the produced messages, is traced accordingly.

## Additional Resources

* <xref:batch-processing> guide
* <xref:default-headers> guide
* <xref:producing-tracing> guide
* [Distributed Tracing](https://www.w3.org/TR/trace-context/) specification
* [Activity](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) class documentation
