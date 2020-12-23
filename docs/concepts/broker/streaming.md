---
uid: streaming
---

# Streaming

<xref:Silverback.Messaging.Messages.IMessageStreamEnumerable`1> is an [IEnumerable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1) and [IAsyncEnumerable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1) that will be asynchronously pushed with the consumed messages.

The <xref:Silverback.Messaging.Messages.IMessageStreamEnumerable`1> can be used to consume the entire topic in a streaming fashion and it's the only way to consume sequences (see for example [batch consuming](xref:inbound#batch-processing)).

```csharp
public class StreamSubscriber
{
    public async Task OnOrderStreamReceived(
        IMessageStreamEnumerable<OrderEvent> eventsStream)
    {
        await foreach(var orderEvent in eventsStream)
        {
            // ...process the event...
        }
    }
}
```

A single instance of <xref:Silverback.Messaging.Messages.IMessageStreamEnumerable`1> is created and published per each topic/partition and the messages are acknowledged (committed) after a single iteration completes, unless sequencing (e.g. batch consuming) is configured or a sequence is automatically recognized by Silverback (e.g. a dataset). In that case an instance is published per each sequence and the entire sequence is atomically committed.

# Rx (Observable)

The [Silverback.Core.Rx](https://www.nuget.org/packages/Silverback.Core.Rx) package adds the <xref:Silverback.Messaging.Messages.IMessageStreamObservable`1> that works like the <xref:Silverback.Messaging.Messages.IMessageStreamEnumerable`1> but implements [IObservable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.iobservable-1) enabling the usage of [Rx.NET](https://github.com/dotnet/reactive).

# [Startup](#tab/rx-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSilverback().AsObservable();
    }
}
```
# [Subscriber](#tab/rx-subscriber)
```csharp
public class StreamSubscriber
{
    public async Task OnOrderStreamReceived(
        IMessageStreamObservable<OrderEvent> eventsStream)
    {
        stream.Subscribe(...);
    }
}
```
***
