---
title: Using the Bus
permalink: /docs/quickstart/bus
---

# Enabling the Bus

The first mandatory step to start using Silverback is to register the internal bus services with the .net core dependency injection.

```c#
public void ConfigureServices(IServiceCollection services)
{
    ...

    services.AddBus()
```

# Creating the Message model

First of all we need to create a message class. All you need to do is to implement the empty `IMessage` interface (or derived interfaces, more about this in the [Message Types]({{ site.baseurl }}/docs/quickstart/message-types) page).

```c#
using Silverback.Messaging.Messages;

public class SampleMessage : IMessage
{
    public string Content { get; set; }
}

```

# Publishing

To publish the message you just need an instance of `IPublisher` (or derived interfaces, more about his in the MessageTypes page).

```c#
using Silverback.Messaging.Publishing;

public class PublishingService
{
    private readonly IPublisher _publisher;

    public MyService(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishSomething()
    {
        await _publisher.PublishAsync(new SampleMessage 
        { 
            Content = "whatever"
        });
    }
}

```

# Subscribing

Now all is left to do is write a subscriber method and register the subscribing class with the DI container.

```c#
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
```c#
public void ConfigureServices(IServiceCollection services)
{
    ...

    services
        .AddBus()
        .AddScoped<ISubscriber, SubscribingService>();
```

The subscribed method can be either synchronous or asynchronous and it must have a single parameter of type `IMessage` or derived type. The library will figure out which method is to be called for which message according to the type of the parameter.

A subscribed method can optionally return an `IMessage` or an `IEnumerable<IMessage>`, it that case the returned messages are automatically republished to the internal bus.

Note that the subscribing class has to be registered as `ISubscriber` to work with Silverback.

For this use case you only need **Silverback.Core**.