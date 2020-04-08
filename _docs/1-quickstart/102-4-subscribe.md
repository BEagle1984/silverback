---
title: Subscribing
permalink: /docs/quickstart/subscribe
---

Now all is left to do is write a subscriber method to process the produced messages.

## Type based subscription

The default and usually preferred way to subscribe is by implementing the marker interface `ISubscriber`.

``` csharp
using Silverback.Messaging.Subscribers;

public class SubscribingService : ISubscriber
{
    public async Task OnMessageReceived(SampleMessage message)
    {
        // ...your message handling loging...
    }
}
```
<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .AddScopedSubscriber<SubscribingService>();
    }
}
{% endhighlight %}
</figure>

All subscribers must be registered with the service provider as shown in the second code snippet above and all public methods are automatically subscribed by default (see  the [explicit method subscription](#explicit-method-subscription) chapter, if more control over is desired).

All `Add*Subscriber` methods are available also as extensions to the `IServiceCollection` and it isn't therefore mandatory to call them immediately after `AddSilverback`.
{: .notice--note}

### Registering types not implementing ISubscriber

If you don't want to implement `ISubscriber` you can register other types (directly or using a base classe or interface).

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .AddScopedSubscriber<SubscribingService>();
    }
    
    public void Configure(BusConfigurator busConfigurator)
    {
        busConfigurator
            .Subscribe<SubscribingService>();
    }
}
{% endhighlight %}
</figure>
...or...
<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .AddScopedSubscriber<ICustomSubscriber, SubscribingService1>()
            .AddScopedSubscriber<ICustomSubscriber, SubscribingService2>()
    }
    
    public void Configure(BusConfigurator busConfigurator)
    {
        busConfigurator.Subscribe<ICustomSubscriber>();
    }
}
{% endhighlight %}
</figure>

This could be useful to avoid a reference to Silverback in lower layers.
{: .notice--note}

### Explicit method subscription

You can explicitely subscribe a method using the `SubscribeAttribute` (this allows you to subscribe non-public methods as well).

```csharp
using Silverback.Messaging.Subscribers;

public class SubscribingService : ISubscriber
{
    [Subscribe]
    public async Task OnMessageReceived(SampleMessage message)
    {
        // ...your message handling loging...
    }
}
```

The `SubscribeAttribute` exposes three extra properties, that can be used to enable parallelism:
* `Exclusive`: A boolean value indicating whether the method can be executed concurrently to other methods handling the **same message**. The default value is `true` (the method will be executed sequentially to other subscribers).
* `Parallel`: A boolean value indicating whether the method can be executed concurrently when multiple messages are fired at the same time (e.g. in a batch). The default value is `false` (the messages are processed sequentially).
* `MaxDegreeOfParallelism`: Limit the number of messages that are processed concurrently. Used only together with `Parallel = true` and mostly useful when performing CPU-bound work (as opposed to non-blocking I/O). The default value is `Int32.Max` and means that there is no limit to the degree of parallelism.

```csharp
public class SubscribingService : ISubscriber
{
    [Subscribe(Parallel = true, MaxDegreeOfParallelism = 10)]
    public async Task OnMessageReceived(SampleMessage message)
    {
        // ...your message handling loging...
    }
}
```

It is also possible to completely disable the automatic subscription of the public methods.

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void Configure(BusConfigurator busConfigurator)
    {
        busConfigurator.Subscribe<ISubscriber>(
            autoSubscribeAllPublicMethods: false);
    }
}
{% endhighlight %}
</figure>

## Delegate based subscription

It is also possible to subscribe an inline lambda or integrate an existing method without having to modify the codebase to add the `SubscribeAttribute`.

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void Configure(BusConfigurator busConfigurator)
    {
        busConfigurator.Subscribe(
            (IReadOnlyCollection<IMessage> message) =>
                HandleMessage(message));
    }
}
{% endhighlight %}
</figure>

Multiple overloads of the `Subscribe` method exist and you can optionally provide a `SubscriptionOptions` instance to enable parallelism (analog to the properties set to the `SubscribeAttribute`).

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void Configure(BusConfigurator busConfigurator)
    {
        busConfigurator.Subscribe(
            (MyMessage message, MyService service) =>
                service.HandleMessage(message),
            new SubscriptionOptions 
            { 
                Parallel = true, 
                Exclusive = false 
            });
    }
}
{% endhighlight %}
</figure>

## Supported methods and parameters

The subscribed method can either be synchronous or asynchronous, but the asynchronous approach should be preferred if aiming at taking advantage of non-blocking I/O.

The first parameter must be the message or the collection of messages.
The following collection are supported:
* `IEnumerable<TMessage>` or `IReadOnlyCollection<TMessage>`: To be able to handle a batch of messages at once. It will receive also the single messages (in a collection with a single item). (Silverback will in any case always forward a materialized `IList` of messages, but explicitly declaring the paramter as `IReadOnlyCollection<T>` avoids any false positive *"possible multiple enumeration of IEnumerable"* issue that may be detected by a static code analysis tool.)
* `Observable<TMessage>`: `Silverback.Core.Rx` allows you to handle your messages in a reactive programming fashion.

Using a collection as parameter allows you to handle a batch of messages at once, allowing more control. The methods with a collection as parameter will still be called for single messages and methods with a single message as input parameter will be called for each message in a batch (in parallel, if allowed by the specified configuration).

```csharp
using Silverback.Messaging.Subscribers;

public class SubscribingService : ISubscriber
{
    [Subscribe(Parallel=true)]
    public async Task OnMessageReceived(Observable<SampleMessage> stream) =>
        stream...Subscribe(...);
}
```

The method can have other parameters that will be resolved using the service provider. Very useful also to integrate existing code.

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void Configure(BusConfigurator busConfigurator)
    {
        busConfigurator.Subscribe(
            (BasketCheckoutMessage message, CheckoutService service) => 
                service.Handle(message));
    }
}
{% endhighlight %}
</figure>

## Return values

A subscriber can also have a return value that can be collected by the publisher.

```csharp
using Silverback.Messaging.Subscribers;

public class SubscribingService : ISubscriber
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

Silverback recognizes per default only the messages implementing `IMessage` but you can register your own types.

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void Configure(BusConfigurator busConfigurator)
    {
        busConfigurator
            .HandleMessagesOfType<MyCustomType>();
    }
}
{% endhighlight %}
</figure>

### Using assembly scanning

You may use a Dependency Injection framework such as [Autofac](https://autofac.org/) providing assembly scanning.

You can of course use such framework to register the subscribers, the only thing to keep in mind is that they need to be registered both as the marker interface (`ISubscriber`, unless configured otherwise) and as the type itself.

Example using Autofac:
```csharp
public class SubscribersModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .Where(t => t.IsAssignableTo<ISubscriber>())
            .AsImplementedInterfaces()
            .AsSelf()
            .InstancePerLifetimeScope();
    }
}
```

## Bootstrapping

The very first publish will take a bit longer and use more resources, since all subscribers have to be resolved (instantiated) once, in order for Silverback to scan the subscriber methods and figure out which message type is handled.

This operation can be performed at startup, preloading the necessary information. 

It will of course still cause all subscribers to be instantiated, but it's done in a more predictable and controlled way, without affecting the application performance later on (e.g. when handling the first HTTP request).

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
public class Startup
{
    public void Configure(BusConfigurator busConfigurator)
    {
        busConfigurator.ScanSubscribers();
    }
}
{% endhighlight %}
</figure>