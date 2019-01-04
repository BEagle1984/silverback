---
title: Rx.NET
permalink: /docs/quickstart/rx
---

Using the **Silverback.Core.Rx** package it is possible to create an `Observable` over the internal message bus and use [Rx.NET](https://github.com/dotnet/reactive) to handle more advanced use cases.

Enabling the feature is very simple and just requires an extra call to `AddMessageObservable` during the services configuration.

```c#
public void ConfigureServices(IServiceCollection services)
{
    ...

    services
        .AddBus()
        .AddMessageObservable();
```

`AddMessageObservable` registers the injection of `IMessageObservable<>`.

```c#
public class RxSubscriber : IDisposable
{
    private readonly IDisposable _subscription;

    public RxSubscriber(IMessageObservable<IEvent> observable)
    {
        _subscription = observable.Subscribe(HandleMessage);
    }

    public void HandleMessage(IEvent message)
    {
        ...
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
```