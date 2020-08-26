---
uid: subscribe
---

# Subscribing

Now all is left to do is write a subscriber method to process the published messages.

## Introduction

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
> It is perfectly fine to have multiple subscribers handling the same message.

## Subscriber class

The default and usually preferred way to subscribe is to implement the message handling logic into a subscriber class. Such class can declare one or more public message handler methods that are automatically subscribed.

All subscribers must be registered with the service provider as shown in the following example.

# [Subscriber](#tab/type-subscribingservice)
``` csharp
using Silverback.Messaging.Subscribers;

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
# [Startup](#tab/type-startup)
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
***

### Subscription options

The subscriber methods can be decorated with the `SubscribeAttribute` to:
* subscribe non-public methods
* enable parallism through the extra configuration properties
    * `Exclusive`: A boolean value indicating whether the method can be executed concurrently to other methods handling the **same message**. The default value is `true` (the method will be executed sequentially to other subscribers).
    * `Parallel`: A boolean value indicating whether the method can be executed concurrently when multiple messages are fired at the same time (e.g. in a batch). The default value is `false` (the messages are processed sequentially).
    * `MaxDegreeOfParallelism`: Limit the number of messages that are processed concurrently. Used only together with `Parallel = true` and mostly useful when performing CPU-bound work (as opposed to non-blocking I/O). The default value is `Int32.Max` and means that there is no limit to the degree of parallelism.

# [Basic](#tab/attribute-subscribingservice1)
```csharp
using Silverback.Messaging.Subscribers;

public class SubscribingService
{
    [Subscribe]
    public async Task OnMessageReceived(SampleMessage message)
    {
        // TODO: Process message
    }
}
```
# [Parallel](#tab/attribute-subscribingservice2)
```csharp
public class SubscribingService
{
    [Subscribe(Parallel = true, MaxDegreeOfParallelism = 10)]
    public async Task OnMessageReceived(SampleMessage message)
    {
        // TODO: Process message
    }
}
```
***

It is also possible to disable the automatic subscription of the public methods.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .AddScopedSubscriber<SubscribingService>(autoSubscribeAllPublicMethods: false)
    }
}
```
## Base class or interface subscription

You may use a Dependency Injection framework such as [Autofac](https://autofac.org/) providing assembly scanning or you may simple want to be free to the register your subscribers without using the Silverback specific extensions methods. In that case you can register a base class or interface as subscriber, meaning that all matching types registered for dependency injection will automatically be subscribed.

Here an example using Autofac:

# [Startup](#tab/interface-subscription-startup)
``` csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .AddSubscribers<ISubscribingService>();

        ...
    }
}
```
# [Autofac Module](#tab/interface-subscription-autofac-module)
```csharp
public class SubscribersModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .Where(t => t.IsAssignableTo<ISubscribingService>())
            .AsImplementedInterfaces()
            .AsSelf()
            .InstancePerLifetimeScope();
    }
}
```
# [Subscriber](#tab/interface-subscription-subscriber)
```csharp
public class OrderService : ISubscribingService
{
    public void OnOrderCreated(OrderCreatedEvent message)
    {
        ...
    }
}
```
***

> [!Important]
> For this scenario it is important that each subscriber class is registered both as the subscribed base type (`ISubscribingService` in the example above) and as the class itself. Note both `AsImplementedInterfaces` and `AsSelf` in the example above.

## Delegate based subscription

In some cases you may prefer to subscribe a method delegate (or an inline lambda) directly using the `AddDelegateSubscribe` method. Multiple overloads exist and you can optionally provide a `SubscriptionOptions` instance to enable parallelism (analog to the properties set to the `SubscribeAttribute`).

# [Basic](#tab/delegate-startup1)
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
# [Parallel](#tab/delegate-startup2)
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
    
    public void Configure(IBusConfigurator busConfigurator)
    {
        busConfigurator.Subscribe(
            (MyMessage message, MyService service) => service.HandleMessage(message),
            new SubscriptionOptions 
            { 
                Parallel = true, 
                Exclusive = false 
            });
    }
}
```
***

## Supported methods and parameters

The subscribed method can either be synchronous or asynchronous (returning a `Task`).

The first parameter must be the message or the collection of messages that are being handled. The parameter type can be the specific message, a base class or an implemented interface.

The following collection are supported:
* `IEnumerable<TMessage>` or `IReadOnlyCollection<TMessage>`: To be able to handle a batch of messages at once. It will receive also the single messages (in a collection with a single item). (Silverback will in any case always forward a materialized `IList` of messages, but explicitly declaring the paramter as `IReadOnlyCollection<T>` avoids any false positive *"possible multiple enumeration of IEnumerable"* issue that may be detected by a static code analysis tool.)
* `Observable<TMessage>`: `Silverback.Core.Rx` allows you to handle your messages in a reactive programming fashion.

Using a collection as parameter allows you to handle a batch of messages at once. The methods with a collection as parameter will still be called for single messages and methods with a single message as input parameter will be called for each message in a batch (in parallel, if allowed by the specified configuration).

# [Enumerable](#tab/methods-subscribingservice1)
```csharp
using Silverback.Messaging.Subscribers;

public class SubscribingService
{
    public async Task OnMessageReceived(IEnumerable<SampleMessage> messages)
    {
        // TODO: Process messages
    }
}
```
# [Collection](#tab/methods-subscribingservice2)
```csharp
using Silverback.Messaging.Subscribers;

public class SubscribingService
{
    public async Task OnMessageReceived(IReadOnlyCollection<SampleMessage> messages)
    {
        // TODO: Process messages
    }
}
```
# [Observable](#tab/methods-subscribingservice3)
```csharp
using Silverback.Messaging.Subscribers;

public class SubscribingService
{
    public async Task OnMessageReceived(Observable<SampleMessage> stream) =>
        stream...Subscribe(...);
}
```
# [Delegate subscription](#tab/params-delegate)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .AddDelegateSubscriber(
                (IReadOnlyCollection<SampleMessage> messages) =>
                {
                    // TODO: Process messages
                });
    }
}
```
***

The method can have other parameters that will be resolved using the service provider. Most useful to integrate existing code subscribing via a delegate.

# [Delegate](#tab/additional-args-delegate)
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
# [Subscriber](#tab/additional-args-subscriber)
```csharp
public class SubscribingService
{
    public async Task OnMessageReceived(BasketCheckoutMessage message, CheckoutService service)
    {
        service.Checkout(message.BaksetId, message.UserId)
    }
}
```
***

## Return values

A subscriber can also have a return value that can be collected by the publisher.

```csharp
using Silverback.Messaging.Subscribers;

public class SubscribingService
{
    public async Task<SampleResult> OnMessageReceived(SampleMessage message)
    {
        ...

        return new SampleResult(...);
    }
}
```

## Return new messages (republishing)

A subscribed method can also optionally return a message or a collection of messages (either `IEnumerable<TMessage>`, `IReadOnlyCollection<TMessage>` or `Observable<TMessage>`, if using `Silverback.Core.Rx`) that will be automatically republished to the internal bus.

# [Single](#tab/republish-subscribingservice1)
```csharp
using Silverback.Messaging.Subscribers;

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
# [Multiple](#tab/republish-subscribingservice2)
```csharp
using Silverback.Messaging.Subscribers;

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
***

Silverback recognizes per default only the messages implementing `IMessage` but you can register your own types (you can register base types and interfaces as well).

# [Startup](#tab/republishcustom-startup)
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
# [Subscriber](#tab/republishcustom-subscribingservice)
```csharp
using Silverback.Messaging.Subscribers;

public class SubscribingService
{
    public async Task<CustomSampleMessage> OnMessageReceived(SampleMessage message)
    {
        ...

        return new CustomSampleMessage
        {
            ...
        };
    }
}
```
# [Message Model](#tab/republishcustom-customsamplemessage)
```csharp
public class CustomSampleMessage : ICustomMessage
{
    public string SomeProperty { get; set; }
}
```
***
