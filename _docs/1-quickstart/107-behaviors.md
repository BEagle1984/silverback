---
title: Behaviors
permalink: /docs/quickstart/behaviors
toc: true
---

The behaviors can be used to build a custom pipeline (similar to the asp.net pipeline), easily adding your cross-cutting functionalities such as logging, validation, etc.

## IBehavior

The behaviors implementing the `IBehavior` interface will be invoked by the `IPublisher` internals every time a message is published to the internal bus (this includes the inbound/outbound messages, but they will be wrapped into an `IInboundEvelope` or `IOutboundEnvelope`).

At every call to `IPublisher.Publish` the `Handle` method of each registered behavior is called, passing in the collection of messages and the delegate to the next step in the pipeline. This gives you the flexibility to execute any sort of code before and after the messages have been actually published (before or after calling the `next()` step). You can for example modify the messages before publishing them, validate them (like in the above example), add some logging / tracing, etc.

### IBehavior example

The following example demonstrates how to use a behavior to trace the messages.

```c#
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

**Note:** The `Handle` receives a collection of `object` because a bunch of messages can be published at once via `IPublisher` or the consumer can be configured to process the messages in batch.
{: .notice--info}

**Note:** `IInboundEnvelope` and `IOutboundEnvelope` are internally used by Silverback to wrap the messages being sent to or received from the message broker and will be received by the `IBroker`. Those interfaces contains the message plus the additional data like endpoint, headers, offset, etc.
{: .notice--info}

The `IBehavior` implementation have simply to be registered for DI.

```c#
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddSilverback()
        .AddScopedBehavior<TracingBehavior>();
```

**Note:** All `Add*Behavior` methods are available also as extensions to the `IServiceCollection` and it isn't therefore mandatory to call them immediately after `AddSilverback`.
{: .notice--info}


## IProducerBehavior and IConsumerBehavior

The `IProducerBehavior` and `IConsumerBehavior` are similar to the `IBehavior` but work at a lower level, much closer to the message broker.

### IProducerBehavior example

The following example demonstrate how to set a custom message header on each outbound message.

```c#
public class CustomHeadersBehavior : IProducerBehavior
{
    public async Task Handle(RawBrokerMessage message, IProducer producer, RawOutboundEnvelopeHandler next)
    {
        message.Headers.Add("generated-by", "silverback");
    }
}
```

### IConsumerBehavior example

The following example demonstrate how to log the headers received with each inbound message.

```c#
public class LogHeadersBehavior : IConsumerBehavior
{
    private readonly ILogger<LogHeadersBehavior> _logger;

    public LogHeadersBehavior(ILogger<LogHeadersBehavior> logger)
    {
        _logger = logger;
    }

    public async Task Handle(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            IServiceProvider serviceProvider,
            IConsumer consumer,
            RawInboundEnvelopeHandler next)
    {
        foreach (var header in message.Headers)
        {
            _logger.LogTrace(
                "{key}={value}",
                header.Key,
                header.Value);
        }

        return await next(messages);
    }
}
```

**Note:** The `Handle` method reaceives an instance of `IServiceProvider` that can be either the root service provider or the scoped service provider for the processing of the consumed message (depending on the position of the behavior in the pipeline).
{: .notice--info}

### Limitations

Because of the way the Silverback's broker integration works `IProducerBehavior` and `IConsumerBehavior` implementations can only be registered as singleton. An `IProducerBehaviorFactory` or `IConsumerBehaviorFactory` can be used to create an instance per each `IConsumer` or `IProducer` that gets intantiated.

If a scoped instance is needed you have to either use the `IServiceProvider` or use an `IBehavior` (that can still be used to accomplish most of the tasks, as shown in the next example).

```c#
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
