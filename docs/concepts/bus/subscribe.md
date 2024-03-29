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
> It is perfectly fine to have multiple subscribers handling the same message but you have to be aware that all them will share the same DI scope (and thus the same `DbContext` instance).

## Subscriber class

The default and usually preferred way to subscribe is to implement the message handling logic into a subscriber class. Such class can declare one or more public message handler methods that are automatically subscribed.

All subscribers must be registered with the service provider as shown in the following example.

# [Subscriber](#tab/type-subscribingservice)
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

All public methods are automatically subscribed by default but the <xref:Silverback.Messaging.Subscribers.SubscribeAttribute> can be used to decorate the non-public methods and subscribe them as well.

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

## Delegate based subscription

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

## Supported methods and parameters

The subscribed method can either be synchronous or asynchronous (returning a `Task`).

The first parameter must be the message and the parameter type can be the specific message, a base class or an implemented interface.

Furthermore, when consuming from a message broker, it is possible to subscribe to the message stream and asynchronously enumerate through the messages, as shown in the <xref:streaming> chapter. 

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

A subscribed method can also optionally return a message or a collection of messages (either [IEnumerable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1), [IReadOnlyCollection<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlycollection-1) or [IObservable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.iobservable-1), if using [Silverback.Core.Rx](https://www.nuget.org/packages/Silverback.Core.Rx)) that will be automatically republished to the internal bus.

# [Single](#tab/republish-subscribingservice1)
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
# [Multiple](#tab/republish-subscribingservice2)
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
***

Silverback recognizes per default only the messages implementing <xref:Silverback.Messaging.Messages.IMessage> but you can register your own types (you can register base types and interfaces as well).

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
