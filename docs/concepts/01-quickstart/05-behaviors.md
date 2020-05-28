---
uid: behaviors
---

# Behaviors

The behaviors can be used to build a custom pipeline (similar to the asp.net pipeline), easily adding your cross-cutting functionalities such as logging, validation, etc.

## IBehavior

The behaviors implementing the `IBehavior` interface will be invoked by the `IPublisher` internals every time a message is published to the internal bus (this includes the inbound/outbound messages, but they will be wrapped into an `IInboundEvelope` or `IOutboundEnvelope`).

At every call to `IPublisher.Publish` the `Handle` method of each registered behavior is called, passing in the collection of messages and the delegate to the next step in the pipeline. This gives you the flexibility to execute any sort of code before and after the messages have been actually published (before or after calling the `next()` step). You can for example modify the messages before publishing them, validate them (like in the above example), add some logging / tracing, etc.

The `IBehavior` implementation have simply to be registered for DI.

### IBehavior example

The following example demonstrates how to use a behavior to trace the messages.

# [Behavior](#tab/ibehavior)
```csharp
using Silverback.Messaging.Publishing;

public class TracingBehavior : IBehavior
{
    private readonly ITracer _tracer;

    public TracingBehavior(ITracer tracer)
    {
        _tracer = tracer;
    }

    public async Task<IReadOnlyCollection<object>> Handle(
        IReadOnlyCollection<object> messages, 
        MessagesHandler next)
    {
        tracer.TraceProcessing(messages);
        var result = await next(messages);
        tracer.TraceProcessed(messages);

        return result;
    }
}
```
# [Startup](#tab/ibehavior-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .AddScopedBehavior<TracingBehavior>();
    }
}
```
***

> [!Note]
> The `Handle` receives a collection of `object` because a bunch of messages can be published at once via `IPublisher` or the consumer can be configured to process the messages in batch.

> [!Note]
>`IInboundEnvelope` and `IOutboundEnvelope` are internally used by Silverback to wrap the messages being sent to or received from the message broker and will be received by the `IBroker`. Those interfaces contains the message plus the additional data like endpoint, headers, offset, etc.

> [!Note]
> All `AddBehavior` methods are available also as extensions to the `IServiceCollection` and it isn't therefore mandatory to call them immediately after `AddSilverback`.

## IProducerBehavior and IConsumerBehavior

The `IProducerBehavior` and `IConsumerBehavior` are similar to the `IBehavior` but work at a lower level, much closer to the message broker.

### IProducerBehavior example

The following example demonstrate how to set a custom message header on each outbound message.

# [ProducerBehavior](#tab/producerbehavior)
```csharp
public class CustomHeadersProducerBehavior : IProducerBehavior
{
    public async Task Handle(
        ProducerPipelineContext context, 
        RawOutboundEnvelopeHandler next)
    {
        context.Envelope.Message.Headers.Add("generated-by", "silverback");

        await next(context);
    }
}
```
# [Startup](#tab/producerbehavior-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToKafka(options => options
                .AddSingletonBrokerBehavior<CustomHeadersBehavior>()
            );
    }
}
```
***

### IConsumerBehavior example

The following example demonstrate how to log the headers received with each inbound message.

# [ConsumerBehavior](#tab/consumerbehavior)
```csharp
public class LogHeadersConsumerBehavior : IConsumerBehavior
{
    private readonly ILogger<LogHeadersBehavior> _logger;

    public LogHeadersBehavior(ILogger<LogHeadersBehavior> logger)
    {
        _logger = logger;
    }

    public async Task Handle(
        ConsumerPipelineContext context, 
        IServiceProvider serviceProvider,
        RawInboundEnvelopeHandler next)
    {
        foreach (var envelope in context.Envelopes)
        {
            foreach (var header in envelope.Headers)
            {
                _logger.LogTrace(
                    "{key}={value}",
                    header.Key,
                    header.Value);
            }
        }

        await next(context, serviceProvider);
    }
}
```
# [Startup](#tab/consumerbehavior-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToKafka(options => options
                .AddSingletonBrokerBehavior<LogHeadersBehavior>()
            );
    }
}
```
***

> [!Note]
> The `Handle` method reaceives an instance of `IServiceProvider` that can be either the root service provider or the scoped service provider for the processing of the consumed message (depending on the position of the behavior in the pipeline).

### Limitations

Because of the way the Silverback's broker integration works `IProducerBehavior` and `IConsumerBehavior` implementations can only be registered as singleton. An `IProducerBehaviorFactory` or `IConsumerBehaviorFactory` can be used to create an instance per each `IConsumer` or `IProducer` that gets intantiated.

If a scoped instance is needed you have to either inject the `IServiceScopeFactory` (or `IServiceProvider`) or use an `IBehavior` (that can still be used to accomplish most of the tasks, as shown in the next examples).

```csharp
public class TracingBehavior : IBehavior
{
    private readonly IDbLogger _dbLogger;

    public TracingBehavior(IDbLogger _dbLogger)
    {
        _dbLogger = dbLogger;
    }

    public async Task<IReadOnlyCollection<object>> Handle(
        IReadOnlyCollection<object> messages, 
        MessagesHandler next)
    {
        foreach (var envelope in messages.OfType<IInboundEnvelope>())
        {
            _dbLogger.LogInboundMessage(
                envelope.Message.GetType(), 
                envelope.Headers,
                envelope.Endpoint,
                envelope.Offset);
        }

        await _dbLogger.SaveChangesAsync();

        return await next(messages);
    }
}
```

## Sorting

The order in which the behaviors are executed does obviously matter and it is possible to precisely define it implementing the `ISorted` interface.

```csharp
public class SortedBehavior : IBehavior, ISorted
{
    public int SortIndex => 120;

    public Task<IReadOnlyCollection<object>> Handle(
        IReadOnlyCollection<object> messages, 
        MessagesHandler next)
    {
        // ...your logic...

        return next(messages);
    }
}
```

The sort index of the built-in behaviors is described in the next chapter.

## Built-in behaviors

Silverback itself strongly relies on the behaviors to implement its features and combine them all together. In this chapter you find the list of the existing behaviorS and their respective sort index.

### IBehavior

This behaviors act in the internal bus pipeline.

Name | Index | Description
:-- | --: | :--
`OutboundProducerBehavior` | 200 | Produces the `IOutboundEnvelope<TMessage>` through the correct `IOutboundConnector` instance.
`OutboundRouterBehavior` | 300 | Routes the messages to the outbound endpoint by wrapping them in an `IOutboundEnvelope<TMessage>` that is republished to the bus.

The sort index is counterintoutive, as the `OutboundRouterBehavior` is actually needed **before** the `OutboundProducerBehavior`, but that happen in two separate and consecutive pipelines to give you the chance to subscribe to the `IOutboundEnvelope` if needed. So the `OutboundRouterBehavior` creates the `IOutboundEnvelope` and publishes it to the internal bus for the next `OutboundProducerBehavior` to catch it and forward it to the configured `IOutboundConnector` (at this point the message is not forwaded anymore to the next behavior in the pipeline).
{: .notice--note}

### IProducerBehavior

This behaviors build the producer pipeline and contain the actual logic to properly serialize the messages according to the applied configuration.

Name | Index | Description
:-- | --: | :--
`ActivityProducerBehavior` | 100 | Starts an `Activity` and adds the tracing information to the message headers.
`HeadersWriterProducerBehavior` | 200 | Maps the properties decorated with the `HeaderAttribute` to the message headers.
`MessageIdInitializerProducerBehavior` | 300 | It ensures that an x-message-id header is always produced.
`BrokerKeyHeaderInitializer` | 310 | Provided by the message broker implementation (e.g. `KafkaMessageKeyInitializerProducerBehavior` or `RabbitRoutingKeyInitializerProducerBehavior`), sets the message key header that will be used by the `IProducer` implementation to set the actual message key.
`BinaryFileHandlerProducerBehavior` | 500 | Switches to the `BinaryFileMessageSerializer` if the message being produced implements the `IBinaryFileMessage` interface.
`SerializerProducerBehavior` | 900 | Serializes the message being produced using the configured `IMessageSerializer`.
`EncryptorProducerBehavior` | 950 | Encrypts the message according to the `EncryptionSettings`.
`ChunkSplitterProducerBehavior` | 1000 | Splits the messages into chunks according to the `ChunkSettings`.

### IConsumerBehavior

This behaviors are the foundation of the consumer pipeline and contain the actual logic to deserialize the incoming messages.

Name | Index | Description
:-- | --: | :--
`ActivityConsumerBehavior` | 100 | Starts an `Activity` with the tracing information from the message headers.
`InboundProcessorConsumerBehavior` | 200 | Handles the retry policies, batch consuming and scope management of the messages that are consumed via an inbound connector.
`ChunkAggregatorConsumerBehavior` | 300 | Temporary stores and aggregates the message chunks to rebuild the original message.
`DecryptorConsumerBehavior` | 400 | Decrypts the message according to the `EncryptionSettings`.
`BinaryFileHandlerProducerBehavior` | 500 | Switches to the `BinaryFileMessageSerializer` if the message being consumed is a binary message (according to the `x-message-type` header.
`DeserializerConsumerBehavior` | 600 | Deserializes the messages being consumed using the configured `IMessageSerializer`.
`HeadersReaderConsumerBehavior` | 700 | Maps the headers with the properties decorated with the `HeaderAttribute`.
