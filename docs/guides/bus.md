---
uid: bus
---

# Using the Message Bus

The message bus is the central component of Silverback that allows you to publish messages and have them delivered to the appropriate subscribers.

## Creating the Message Model

First, we need to create a message class. The message class can be any POCO class. Any CLR type can be used or referenced since messages are only exchanged in memory, making it similar to calling a method directly.

```csharp
public class SampleMessage
{
    public string Content { get; set; }
}
```

It is not mandatory, but it is recommended to use the [Silverback.Core.Model](https://www.nuget.org/packages/Silverback.Core.Model) package (documented in the next chapter) to enhance semantics and improve code readability.

### Silverback.Core.Model

Dedicated interfaces for events, commands, and queries are available in [Silverback.Core.Model](https://www.nuget.org/packages/Silverback.Core.Model) to help define the meaning of each message, making the code more structured and readable.

The _integration_ variations are designed for messages exchanged via a message broker like Apache Kafka or MQTT.

These are the available interfaces:

- <xref:Silverback.Messaging.Messages.IEvent> is used to notify other services of an event that has occurred. Events are fire-and-forget messages, meaning no response is expected.
- <xref:Silverback.Messaging.Messages.ICommand> and <xref:Silverback.Messaging.Messages.ICommand`1> are used to trigger actions in another service or component. Commands are typically consumed by a single subscriber and can return a value (of type `TResult`).
- <xref:Silverback.Messaging.Messages.IQuery`1> functions similarly to <xref:Silverback.Messaging.Messages.ICommand`1> but always returns a result, as it represents a data request.
- The <xref:Silverback.Messaging.Messages.IIntegrationMessage> interface identifies messages exchanged through a message broker. It has two specialized variations: <xref:Silverback.Messaging.Messages.IIntegrationEvent> and <xref:Silverback.Messaging.Messages.IIntegrationCommand>.

## Publishing Messages

To publish a message, you need an instance of <xref:Silverback.Messaging.Publishing.IPublisher>, which is registered with the DI container as a transient service.

```csharp
using Silverback.Messaging.Publishing;

public class MyPublishingService
{
    private readonly IPublisher _publisher;

    public MyPublishingService(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishSomething()
    {
        SampleMessage message = new SampleMessage
        {
            Content = "Silverback rocks!"
        };
        await _publisher.PublishAsync(message);
    }
}
```

The publisher provides both synchronous and asynchronous versions of each method.

> [!Important]
> The publisher is a transient service, meaning it can be resolved from the root provider and injected into singleton services, but it will throw an exception if the published message has to be handled by a scoped subscriber.
> Producing to a message broker does *not* require a scope and is therefore safe to be done from a singleton service.

### Return Values

Subscribers can return a result after processing a message.

```csharp
public async Task<QueryResult> PublishSomething()
{
    MyQuery query = new MyQuery() { ... };
    QueryResult result = await _publisher.PublishAsync(query);
    return result.Single();
}
```

> [!Note]
> The call to `Single()` is required because Silverback allows multiple subscribers for the same message, collecting multiple return values. This is unnecessary when using the specialized publishers described in the next chapter. The `ICommand` and `IQuery` interfaces specify the `TResult` type for better clarity.

### Silverback.Core.Model Extensions

Each message type (<xref:Silverback.Messaging.Messages.IEvent>, <xref:Silverback.Messaging.Messages.ICommand>/<xref:Silverback.Messaging.Messages.ICommand`1>, and <xref:Silverback.Messaging.Messages.IQuery`1>) includes specialized extensions for <xref:Silverback.Messaging.Publishing.IPublisher> to improve semantics and clarity.

```csharp
public async Task PublishEvent()
{
    MyEvent myEvent = new MyEvent() { ... };
    await _publisher.PublishEventAsync(myEvent);
}

public async Task ExecuteCommand()
{
    MyCommand myCommand = new MyCommand() { ... };
    await _publisher.ExecuteCommandAsync(myCommand);
}
```

## Subscribing to Messages

Now, we need to write a subscriber method to process the published messages.

Silverback’s message bus routes messages based on their type. When a message is published, Silverback evaluates the signatures of the subscribed methods and invokes those accepting the specific message type, a base type, or an implemented interface.

### Subscriber Class

The preferred way to subscribe is by implementing message handling logic in a dedicated subscriber class.

```csharp
public class SubscribingService
{
    public async Task OnMessageReceived(SampleMessage message)
    {
        // Process message
    }
}
```

The subscriber class must be registered with the DI container using the `AddScopedSubscriber`, `AddSingletonSubscriber`, or `AddTransientSubscriber` extension methods.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .AddScopedSubscriber<SubscribingService>();
    }
}
```

By default, all public methods in a registered subscriber class (excluding the ones declared in the based classes) are subscribed. To subscribe non-public methods, or methods from the base classes, or customize the subscription options, use the <xref:Silverback.Messaging.Subscribers.SubscribeAttribute> to decorate the methods.

You can also disable automatic subscription of public methods:

```csharp
services
    .AddSilverback()
    .AddScopedSubscriber<SubscribingService>(autoSubscribeAllPublicMethods: false);
```

### Delegate based subscription

In some cases you may prefer to subscribe a method delegate (or an inline lambda) directly using the `AddDelegateSubscriber` method.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .AddDelegateSubscriber((SampleMessage message) =>
            {
                // TODO: Process messages
            });
    }
}
```

### Supported methods and parameters

The subscribed method (or delegate) can either be synchronous or asynchronous (returning a `Task` or a `ValueTask`).

The first parameter must be the message and the parameter type can be the specific message, a base class or an implemented interface.

The method can have other parameters that will be resolved using the service provider, or the cancellation token (see [Cancellation](#cancellation)).

```csharp
public class SubscribingService
{
    public async Task OnMessageReceived(BasketCheckoutMessage message, CheckoutService service)
    {
        service.Checkout(message.BaksetId, message.UserId);
    }
}
```

or

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .AddDelegateSubscriber(
                (BasketCheckoutMessage message, CheckoutService service) =>
                {
                    service.Checkout(message.BaksetId, message.UserId);
                });
    }
}
```

### Return values

A subscriber can also have a return value that can be collected by the publisher.

```csharp
public class SubscribingService
{
    public async Task<SampleResult> OnMessageReceived(SampleMessage message)
    {
        ...

        return new SampleResult(...);
    }
}
```

Ideally, you should use the specialized interfaces <xref:Silverback.Messaging.Messages.ICommand`1> and <xref:Silverback.Messaging.Messages.IQuery`1> to define the return type and make it easier to handle the result.

### Return New Messages (Republishing)

The subscriber method can also return a message or a collection of messages (either [IEnumerable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1), [IReadOnlyCollection<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlycollection-1) or [IObservable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.iobservable-1), if using [Silverback.Core.Rx](https://www.nuget.org/packages/Silverback.Core.Rx)) that will be automatically republished to the internal bus.

```csharp
public class SubscribingService
{
    public async Task<OtherSampleMessage> OnMessageReceived(SampleMessage message)
    {
        ...

        return new OtherSampleMessage
        {
            ...
        };
    }
}
```

or

```csharp
public class SubscribingService
{
    public IEnumerable<IMessage> OnMessageReceived(IEnumerable<SampleMessage> messages) =>
        messages.SelectMany(message =>
        {
            yield return new OtherSampleMessage1
            {
                ...
            };
            yield return new OtherSampleMessage2
            {
                ...
            };
        });
}
```

Silverback recognizes only the messages implementing <xref:Silverback.Messaging.Messages.IMessage> as messages to be republished (<xref:Silverback.Messaging.Messages.IEvent>, <xref:Silverback.Messaging.Messages.ICommand>/<xref:Silverback.Messaging.Messages.ICommand`1>, and <xref:Silverback.Messaging.Messages.IQuery`1> all implement that interface), but you can register your own types, base types or interfaces.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .HandleMessagesOfType<ICustomMessage>();
    }
}
```

## Cancellation

If a subscriber method accepts a `CancellationToken`, Silverback will forward the optional cancellation token to the subscribers.

```csharp
await _publisher.ExecuteCommandAsync(myCommand, cancellationToken);
```

The cancellation token can be used to interrupt long-running operations or can be passed to other API such as HTTP requests or database operations supporting cancellation.

```csharp
public async Task OnCommandReceived(MyCommand command, CancellationToken cancellationToken)
{
    while (...) // Long-running operation
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // Processing
    }
}
```

## Behaviors

Behaviors allow you to implement a custom pipeline (similar to the ASP.NET middleware), adding cross-cutting concerns like logging and validation.

```csharp
public class TracingBehavior : IBehavior
{
    private readonly ITracer _tracer;

    public TracingBehavior(ITracer tracer)
    {
        _tracer = tracer;
    }
    
    public async Task<IReadOnlyCollection<object?>> HandleAsync(
        object message,
        MessageHandler next)
    {
        try
        {
            _tracer.TraceProcessing(message);
            object result = await next(message);
            _tracer.TraceProcessed(message);
            return result;
        }
        catch (Exception ex)
        {
            _tracer.TraceError(message, ex);
            throw;
        }
    }
}
```

Register the behavior with the DI container using `AddScopedBehavior`, `AddSingletonBehavior`, or `AddTransientBehavior`.

```csharp
services
    .AddSilverback()
    .AddScopedBehavior<TracingBehavior>();
```

If execution order is important, implement <xref:Silverback.ISorted> and specify the `SortIndex`.

```csharp
public class SortedBehavior : IBehavior, ISorted
{
    public int SortIndex => 120;
    
    public Task<IReadOnlyCollection<object?>> HandleAsync(
        object message,
        MessageHandler next)
    {
        return next(message);
    }
}
```

