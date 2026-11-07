---
uid: logging
---

# Logging and Tracing

Silverback writes its logs through the standard `Microsoft.Extensions.Logging` abstractions, so they flow into whatever logging provider your application uses (console, Serilog, OpenTelemetry, etc.). Each event has a stable `EventId` that can be used to filter or route the logs.

On top of that, Silverback creates [Activity](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) objects to support distributed tracing (see [Tracing](#tracing)).

## Log Events

Silverback logs several events that can be used to monitor the application and troubleshoot issues. The tables below list the events and their default level.

### Core

[!INCLUDE [Core log events](../build/generated/log-events-core.md)]

### Integration

[!INCLUDE [Integration log events](../build/generated/log-events-integration.md)]

### Kafka

[!INCLUDE [Kafka log events](../build/generated/log-events-kafka.md)]

### MQTT

[!INCLUDE [MQTT log events](../build/generated/log-events-mqtt.md)]

### In-Memory Storage

[!INCLUDE [In-memory storage log events](../build/generated/log-events-storage-memory.md)]

## Customizing the Log Levels

The level of each Silverback log event can be overridden using `WithLogLevels`. The level can be set statically or determined dynamically, for example depending on the exception type.

```csharp
services
    .AddSilverback()
    .WithLogLevels(levels => levels
        .SetLogLevel(CoreLogEvents.LockAcquired.EventId, LogLevel.Debug)
        .SetLogLevel(
            IntegrationLogEvents.ProcessingConsumedMessageError.EventId,
            (exception, defaultLevel) => exception is TimeoutException ? LogLevel.Warning : defaultLevel));
```

The event ids are exposed by the <xref:Silverback.Diagnostics.CoreLogEvents>, <xref:Silverback.Diagnostics.IntegrationLogEvents>, <xref:Silverback.Diagnostics.KafkaLogEvents>, <xref:Silverback.Diagnostics.MqttLogEvents> and <xref:Silverback.Diagnostics.StorageMemoryLogEvents> static classes.

> [!Tip]
> To reduce the noise, you can also use the standard logging filters to change the minimum level of the `Silverback` categories.

## Tracing

An [Activity](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) is created:
* in the Consumer when a message is received (initialized with the `traceparent` header, if submitted)
* in the Producer when a message is being sent (submitting the `Activity.Id` in the `traceparent` header)
* when a subscriber is being invoked (either internally or from a consumer)

This allows tracing the message handling and follow it across different services (distributed tracing). See <xref:producing-tracing> and <xref:consuming-tracing> for details about the trace propagation.

The following table summarizes the activities and the tags being set (the tags in square brackets are set only when applicable).

Source | Activity | Description / Tags
:-- | :-- | :--
`Silverback.Integration.Produce` | `Silverback.Integration.Produce` | A message is being produced to a message broker.<br/><br/>Tags:<ul><li>`messaging.destination`</li><li>[`messaging.message_id`] (Kafka only, e.g. `my-topic[2]@42`)</li><li>[`messaging.kafka.message_key`]</li><li>[`messaging.kafka.partition`] (e.g. `my-topic[2]`)</li></ul>
`Silverback.Integration.Consume` | `Silverback.Integration.Consume` | A consumed message is being processed.<br/><br/>Tags:<ul><li>`messaging.destination`</li><li>[`messaging.message_id`] (Kafka only, e.g. `my-topic[2]@42`)</li><li>[`messaging.sequence_id`]</li><li>[`messaging.kafka.message_key`]</li><li>[`messaging.kafka.partition`] (e.g. `my-topic[2]`)</li></ul>
`Silverback.Core.Subscribers` | `Silverback.Core.Subscribers.InvokeSubscriber` | A subscriber is being invoked to process a message.<br/><br/>Tags:<ul><li>`SubscriberType`</li><li>`SubscriberMethod`</li></ul>

> [!Note]
> The produce and consume activities are always started, even if no listener is registered, so that the trace context is propagated and available in the logs. The subscriber activity is started only when the `Silverback.Core.Subscribers` source is listened to.

### Exporting the Traces

Register the Silverback sources to export the activities, for example with [OpenTelemetry](https://opentelemetry.io/docs/languages/dotnet/):

```csharp
services
    .AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(
            "Silverback.Integration.Produce",
            "Silverback.Integration.Consume",
            "Silverback.Core.Subscribers")
        .AddOtlpExporter());
```

### Including the Trace in the Logs

The generic host enables the `TraceId`, `SpanId` and `ParentId` [activity tracking options](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.activitytrackingoptions) by default, adding them to the logging scope. Enable the scopes in the logging provider to write them along with the log entries (including the Silverback ones):

```csharp
builder.Logging.AddSimpleConsole(options => options.IncludeScopes = true);
```

```
info: Silverback.Messaging.Consuming.Transaction.TransactionHandlerConsumerBehavior[1001]
      => SpanId:6b9a3e2c41f0d815, TraceId:4bf92f3577b34da6a3ce929d0e0e4736, ParentId:00f067aa0ba902b7
      Processing consumed message | EndpointName: my-topic, BrokerMessageId: [2]@42
```

Outside the generic host, set the options explicitly:

```csharp
services.AddLogging(logging => logging
    .Configure(options => options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.ParentId)
    .AddSimpleConsole(options => options.IncludeScopes = true));
```

> [!Tip]
> Serilog (3.1 or later) captures the `TraceId` and `SpanId` automatically, so they can be added to the output template (e.g. `{TraceId}`), while the OpenTelemetry logging provider correlates the logs with the traces out of the box.
