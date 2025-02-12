---
uid: mediator
---

# Using the Mediator

The mediator is the central component of Silverback that allows you to publish messages and have them delivered to the appropriate subscribers.


## Creating the Message model

First of all we need to create a message class. The message class can be any POCO class, it just need to be serializable.

```csharp
public class SampleMessage
{
    public string Content { get; set; }
}
```

It is not mandatory, but suggested to consider using the [Silverback.Core.Model](https://www.nuget.org/packages/Silverback.Core.Model) package (documented in the next chapter) for better semanthics and write better and more readable code.

### Silverback.Core.Model

Dedicated interfaces for events, commands and queries are available in [Silverback.Core.Model](https://www.nuget.org/packages/Silverback.Core.Model) to help specify the meaning of each message and produce in better, cleaner and more readable code.

The _integration_ variations are meant to identify those messages produced or consumed via a message broker like Apache Kafka or RabbitMQ.

These are the available interfaces:
* <xref:Silverback.Messaging.Messages.IEvent> is to be used to notify thing that happened inside a service and may be of some interest for one or more other service. The events are a fire-and-forget message type and no response is expected.
* <xref:Silverback.Messaging.Messages.ICommand> or <xref:Silverback.Messaging.Messages.ICommand`1> are used to trigger an action in another service or component and are therefore very specific and usually consumed by one single subscriber. This messages can return a value (of type TResult).
* <xref:Silverback.Messaging.Messages.IQuery`1> works exactly like <xref:Silverback.Messaging.Messages.ICommand`1>. This messages are obviously always returning something since they represent a request for data (query).
* The <xref:Silverback.Messaging.Messages.IIntegrationMessage> interface identifies those messages that are either published to the message broker or received through it and is specialized into <xref:Silverback.Messaging.Messages.IIntegrationEvent> and <xref:Silverback.Messaging.Messages.IIntegrationCommand>.

## Publish

To publish the message you just need an instance of <xref:Silverback.Messaging.Publishing.IPublisher>, which can of course be injected via dependency injection.

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
        } 
        await _publisher.PublishAsync(message);
    }
}
```

The publisher always exposes a synchronous and an asynchronous version of each method.

### Return values

The subscribers are allowed to return a result after having processed the message.

```csharp
public async Task<Report> PublishSomething()
{
    MyQuery query = new MyQuery() { ... };
    var result = await _publisher.PublishAsync(query);
    return result.Single();
}
```

> [!Note]
> Please note the required call to `Single()`, because Silverback allows you to have multiple subscribers for the same message and therefore collect multiple return values. This is not needed if using the specialized publishers described in thenext chapter. The ICommand and IQuery interfaces are also declaring the TResult type to make the code better.

### Silverback.Core.Model

Each message type (<xref:Silverback.Messaging.Messages.IEvent>, <xref:Silverback.Messaging.Messages.ICommand>/<xref:Silverback.Messaging.Messages.ICommand`1> and <xref:Silverback.Messaging.Messages.IQuery`1>) also comes with its specialized extensions for the <xref:Silverback.Messaging.Publishing.IPublisher>, to add better semanthics and make the code cleaner and more explicit.

```csharp
using Silverback.Messaging.Publishing;

public class MyPublishingService
{
    private readonly IPublisher _publisher;

    public MyPublishingService(IPublisher publisher)
    {
        _publisher = publisher;
    }

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
    
    public async Task<MyResult> ExecuteCommandWithResult()
    {
        MyCommand2 command = new MyCommand2() { ... };
        MyResult result = await _publisher.ExecuteCommandAsync(command);
        return result;
    }
    
    public async Task<MyResult> GetResults()
    {
        MyQuery query = new MyQuery() { ... };
        MyQueryResult result = await _publisher.ExecuteQueryAsync(query);
        return result;
    }
}
```

## Subscribing

Now all is left to do is write a subscriber method to process the published messages.

The subscription in the Silverback internal bus is based on the message type. This means that when a message is published Silverback will simply evaluate the signature of the subscribed methods and invoke the ones that accept a message of that specific type, a base type or an implemented interface.

For example, given the following message structure:

```csharp
public abstract class OrderEvent : IEvent
{
    ...
}

public class OrderCreatedEvent : OrderEvent
{
    ...
}
```

All these subscriber methods will be invoked to handle an instance of `OrderCreatedEvent`:
* `void Handle(OrderCreatedEvent message)`
* `void Handle(OrderMessage message)`
* `void Handle(IEvent message)`

> [!Note]
> It is perfectly fine to have multiple subscribers handling the same message but you have to be aware that all them will share the same DI scope.

### Subscriber class

The default and usually preferred way to subscribe is to implement the message handling logic into a subscriber class. Such class can declare one or more public message handler methods that are automatically subscribed.

All subscribers must be registered with the service provider as shown in the following example.

``` csharp
public class SubscribingService
{
    public async Task OnMessageReceived(SampleMessage message)
    {
        // TODO: Process message
    }

    public async Task OnOtherMessageReceived(OtherSampleMessage message)
    {
        // TODO: Process message
    }
}
```

``` csharp
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

### Delegate based subscription

In some cases you may prefer to subscribe a method delegate (or an inline lambda) directly using the [AddDelegateSubscriber](xref:Microsoft.Extensions.DependencyInjection.SilverbackBuilderAddDelegateSubscriberExtensions) method.

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
### Subscription options

All public methods declared in the registered type (excluding base types) are automatically subscribed by default but the <xref:Silverback.Messaging.Subscribers.SubscribeAttribute> can be used to decorate the non-public methods and subscribe them as well.

The <xref:Silverback.Messaging.Subscribers.SubscribeAttribute> can also be used to customize the subscription options, see the attribute properties for details.

It is also possible to disable the automatic subscription of the public methods.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .AddScopedSubscriber<SubscribingService>(
                autoSubscribeAllPublicMethods: false)
    }
}
```
### Supported methods and parameters

The subscribed method can either be synchronous or asynchronous (returning a `Task` or a `ValueTask`).

The first parameter must be the message and the parameter type can be the specific message, a base class or an implemented interface.

Furthermore, when consuming from a message broker, it is possible to subscribe to the message stream and asynchronously enumerate through the messages, as shown in the <xref:streaming> chapter.

The method can have other parameters that will be resolved using the service provider. Most useful to integrate existing code subscribing via a delegate or (maybe even more) if registering the subscriber a singleton but you want to have some dependencies resolved per message and in the scope of the publisher.

```csharp
public class SubscribingService
{
    public async Task OnMessageReceived(BasketCheckoutMessage message, CheckoutService service)
    {
        service.Checkout(message.BaksetId, message.UserId)
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
                    service.Checkout(message.BaksetId, message.UserId)
                });
    }
}
```

### Return values

As mentioned in the Publishing section, a subscriber can also have a return value that can be collected by the publisher.

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

A special case is when the subscribed method can also optionally return a message or a collection of messages (either [IEnumerable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1), [IReadOnlyCollection<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlycollection-1) or [IObservable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.iobservable-1), if using [Silverback.Core.Rx](https://www.nuget.org/packages/Silverback.Core.Rx)) that will be automatically republished to the internal bus.

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

Silverback recognizes per default only the messages implementing <xref:Silverback.Messaging.Messages.IMessage> (IEvent, ICommand and IQuery inherit that of course) as messages to be republished but you can register your own types (you can register base types and interfaces as well).

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

## Behaviors

The behaviors can be used to build a custom pipeline (similar to the asp.net pipeline), easily adding your cross-cutting concerns such as logging, validation, etc.

The behaviors are implementations of the <xref:Silverback.Messaging.Publishing.IBehavior> interface and will be invoked by the <xref:Silverback.Messaging.Publishing.IPublisher> every time a message is published to the internal bus (this includes the inbound/outbound messages, but they will be wrapped into an <xref:Silverback.Messaging.Messages.IInboundEnvelope`1> or <xref:Silverback.Messaging.Messages.IOutboundEnvelope`1>).

The [HandleAsync](xref:Silverback.Messaging.Publishing.IBehavior#Silverback_Messaging_Publishing_IBehavior_HandleAsync_System_Object_Silverback_Messaging_Publishing_MessageHandler_) method of each registered behavior is called every time a message (or a batch of messages) is published to the internal bus, passing in the collection of messages and the delegate to the next step in the pipeline. This gives you the flexibility to execute any sort of code before and after the messages have been actually published (before or after calling the `next` step). You can for example modify the messages before publishing them, validate them (like in the above example), add some logging / tracing, etc.

The <xref:Silverback.Messaging.Publishing.IBehavior> implementation have simply to be registered for DI.

The following example demonstrates how to use a behavior to trace the messages being exchanged via the mediator.

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
        tracer.TraceProcessing(message);
        var result = await next(message);
        tracer.TraceProcessed(message);

        return result;
    }
}
```

The behavior must be registered with the DI container using the Silverback fluent api.

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

The order in which the behaviors are executed might matter and it is possible to precisely define it implementing the <xref:Silverback.ISorted> interface.

```csharp
public class SortedBehavior : IBehavior, ISorted
{
    public int SortIndex => 120;

    public Task<IReadOnlyCollection<object?>> HandleAsync(
        object message, 
        MessageHandler next)
    {
        // ...your logic...

        return next(message);
    }
}
```
