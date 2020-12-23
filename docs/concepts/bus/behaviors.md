---
uid: behaviors
---

# Behaviors

The behaviors can be used to build a custom pipeline (similar to the asp.net pipeline), easily adding your cross-cutting concerns such as logging, validation, etc.

The behaviors are implementations of the <xref:Silverback.Messaging.Publishing.IBehavior> interface and will be invoked by the <xref:Silverback.Messaging.Publishing.IPublisher> every time a message is published to the internal bus (this includes the inbound/outbound messages, but they will be wrapped into an <xref:Silverback.Messaging.Messages.IInboundEnvelope`1> or <xref:Silverback.Messaging.Messages.IOutboundEnvelope`1>).

The [HandleAsync](xref:Silverback.Messaging.Publishing.IBehavior#Silverback_Messaging_Publishing_IBehavior_HandleAsync_System_Collections_Generic_IReadOnlyCollection_System_Object__Silverback_Messaging_Publishing_MessagesHandler_) method of each registered behavior is called every time a message (or a batch of messages) is published to the internal bus, passing in the collection of messages and the delegate to the next step in the pipeline. This gives you the flexibility to execute any sort of code before and after the messages have been actually published (before or after calling the `next` step). You can for example modify the messages before publishing them, validate them (like in the above example), add some logging / tracing, etc.

The <xref:Silverback.Messaging.Publishing.IBehavior> implementation have simply to be registered for DI.

## IBehavior example

The following example demonstrates how to use a behavior to trace the messages.

# [Behavior](#tab/ibehavior)
```csharp
public class TracingBehavior : IBehavior
{
    private readonly ITracer _tracer;

    public TracingBehavior(ITracer tracer)
    {
        _tracer = tracer;
    }

    public async Task<IReadOnlyCollection<object>> HandleAsync(
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
> The [HandleAsync](xref:Silverback.Messaging.Publishing.IBehavior#Silverback_Messaging_Publishing_IBehavior_HandleAsync_System_Collections_Generic_IReadOnlyCollection_System_Object__Silverback_Messaging_Publishing_MessagesHandler_) method receives a collection of `object` because a bunch of messages can be published at once via <xref:Silverback.Messaging.Publishing.IPublisher> or the consumer can be configured to process the messages in batch.

> [!Note]
> <xref:Silverback.Messaging.Messages.IInboundEnvelope> and <xref:Silverback.Messaging.Messages.IOutboundEnvelope> are internally used by Silverback to wrap the messages being sent to or received from the message broker and will be received by the <xref:Silverback.Messaging.Broker.IBroker>. Those interfaces contains the message plus the additional data like endpoint, headers, offset, etc.

### Sorting

The order in which the behaviors are executed might matter and it is possible to precisely define it implementing the <xref:Silverback.ISorted> interface.

```csharp
public class SortedBehavior : IBehavior, ISorted
{
    public int SortIndex => 120;

    public Task<IReadOnlyCollection<object>> HandleAsync(
        IReadOnlyCollection<object> messages, 
        MessagesHandler next)
    {
        // ...your logic...

        return next(messages);
    }
}
```

## See also

<xref:broker-behaviors>
